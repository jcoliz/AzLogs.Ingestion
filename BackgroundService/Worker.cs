using System.Runtime.CompilerServices;
using System.Text.Json;
using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.Options;
using AzLogs.Ingestion.WeatherApiClient;
using Microsoft.Extensions.Options;

namespace AzLogs.Ingestion;

/// <summary>
/// Background worker service which continually runs the logic of this sample
/// </summary>
/// <remarks>
/// Keeps everything running!
/// </remarks>
/// <param name="weatherClient">API client to connect with weather service</param>
/// <param name="logsClient">API client to connect with log collection endpoint</param>
/// <param name="weatherOptions">Options describing where we want the weather</param>
/// <param name="logOptions">Options describing where to send the logs</params>
/// <param name="logger">Where to send application logs</param>
public partial class Worker(
    WeatherClient weatherClient, 
    LogsTransport transport,
    IOptions<WorkerOptions> workerOptions,
    IOptions<WeatherOptions> weatherOptions, 
    ILogger<Worker> logger
    ) : BackgroundService
{
    /// <summary>
    /// Where to send application logs
    /// </summary>
    /// <seealso href="https://adamstorr.co.uk/blog/primary-constructor-and-logging-dont-mix/">
    private readonly ILogger<Worker> _logger = logger;

    /// <summary>
    /// Main loop which continually fetches readings, then sends to Log Analytics
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var forecast = await FetchForecastAsync(stoppingToken).ConfigureAwait(false);
                if (forecast is not null)
                {
                    await transport.UploadToLogsAsync(forecast, stoppingToken).ConfigureAwait(false);
                }
                await Task.Delay(workerOptions.Value.Frequency, stoppingToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Task cancellation is not an error, no action required
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
            var forecast = await weatherClient.Gridpoint_ForecastAsync(
                weatherOptions.Value.Office, 
                weatherOptions.Value.GridX, 
                weatherOptions.Value.GridY, 
                stoppingToken
            )
            .ConfigureAwait(false);

            result = forecast?.Properties.Periods.FirstOrDefault();
            if (result is null)
            {
                logReceivedMalformed();
            }
            else
            {
                logReceivedOk(JsonSerializer.Serialize(result));
            }
        }
        catch (TaskCanceledException)
        {
            // Task cancellation is not an error, no action required
        }
        catch (Exception ex)
        {
            logFail(ex);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Received OK {Result}", EventId = 1010)]
    public partial void logReceivedOk(string result, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Received malformed response", EventId = 1018)]
    public partial void logReceivedMalformed([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Location}: Critical Failure", EventId = 1009)]
    public partial void logCriticalFail(Exception ex, [CallerMemberName] string? location = null);
}
