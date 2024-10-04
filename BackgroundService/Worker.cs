using System.Runtime.CompilerServices;
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
/// <param name="weatherTransport">Transport subsystem which will fetch weather forecasts from NWS</param>
/// <param name="logsClient">Transport subsystem which will send logs to log collection endpoint</param>
/// <param name="workerOptions">Options conrtolling our behaviour</param>
/// <param name="logger">Where to send application logs</param>
public partial class Worker(
    WeatherTransport weatherTransport, 
    LogsTransport logsTransport,
    IOptions<WorkerOptions> workerOptions,
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
                var forecast = await weatherTransport.FetchForecastAsync(stoppingToken).ConfigureAwait(false);
                if (forecast is not null)
                {
                    await logsTransport.UploadToLogsAsync(forecast, stoppingToken).ConfigureAwait(false);
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

    [LoggerMessage(Level = LogLevel.Critical, Message = "{Location}: Critical Failure", EventId = 1009)]
    public partial void logCriticalFail(Exception ex, [CallerMemberName] string? location = null);
}
