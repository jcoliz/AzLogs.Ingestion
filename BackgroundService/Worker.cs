using System.Runtime.CompilerServices;
using System.Text.Json;
using Azure.Monitor.Ingestion;
using Microsoft.Extensions.Options;
using Weather.Worker.Api;
using Weather.Worker.Options;

namespace Weather.Worker;

/// <summary>
/// Background worker service
/// </summary>
/// <remarks>
/// Keeps everything running!
/// </remarks>
/// <param name="client">API client to use to connect with weather service</param>
/// <param name="options">Options describing where we want the weather</param>
/// <param name="logger">Where to log results</param>
public partial class Worker(
    WeatherClient client, 
    LogsIngestionClient logsClient,
    IOptions<WeatherOptions> options, 
    IOptions<LogIngestionOptions> logOptions,
    ILogger<Worker> logger
    ) : BackgroundService
{
    /// <summary>
    /// Where to log results
    /// </summary>
    /// <seealso href="https://adamstorr.co.uk/blog/primary-constructor-and-logging-dont-mix/">
    private readonly ILogger<Worker> _logger = logger;

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var forecast = await FetchForecastAsync(stoppingToken);
                if (forecast is not null)
                {
                    await UploadToLogsAsync(forecast);
                }
                await Task.Delay(options.Value.Frequency, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            logCriticalFail(ex);
        }
    }

    /// <summary>
    /// Fetch forecast from Weather Service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    private async Task<GridpointForecastPeriod?> FetchForecastAsync(CancellationToken stoppingToken)
    {
        GridpointForecastPeriod? result = default;

        try
        {
            var forecast = await client.Gridpoint_ForecastAsync(
                options.Value.Office, 
                options.Value.GridX, 
                options.Value.GridY, 
                stoppingToken
            );
            result = forecast.Properties.Periods.First();
            var json = JsonSerializer.Serialize(result);

            logReceivedOk(json);
        }
        catch (Exception ex)
        {
            logFail(ex);
        }

        return result;
    }

    private async Task UploadToLogsAsync(GridpointForecastPeriod period)
    {
        try
        {
            var response = await logsClient.UploadAsync
            (
                logOptions.Value.DcrImmutableId, 
                logOptions.Value.Stream,
                [ period ]
            )
            .ConfigureAwait(false);

            if (response is null)
            {
                throw new Exception("No response received from server");
            }

            if (response.IsError)
            {
                throw new Exception($"Log upload failed: {response.Status}");
            }

            logSentOk(response.Status);
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Received OK {Result}", EventId = 1010)]
    public partial void logReceivedOk(string result, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Sent OK {Status}", EventId = 1020)]
    public partial void logSentOk(int status, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Location}: Critical Failure", EventId = 1009)]
    public partial void logCriticalFail(Exception ex, [CallerMemberName] string? location = null);
}
