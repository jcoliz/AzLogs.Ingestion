using System.Runtime.CompilerServices;
using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.WeatherTransport;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp;

/// <summary>
/// Container class for Azure Function function body
/// </summary>
/// <param name="weatherTransport">Services to retrieve weather from</param>
/// <param name="logsTransport">Services where to send logs</param>
/// <param name="_logger">Where to send application logs</param>
public partial class TransferLogs(
    WeatherTransport weatherTransport, 
    LogsTransport logsTransport,
    ILogger<TransferLogs> _logger
)
{
    /// <summary>
    /// Where to send application logs
    /// </summary>
    /// <seealso href="https://adamstorr.co.uk/blog/primary-constructor-and-logging-dont-mix/">
    private readonly ILogger logger = _logger;

    /// <summary>
    /// Run the work of this function app one iteration
    /// </summary>
    /// <param name="myTimer">Scheduling information</param>
    [Function("TransferLogs")]
    public async Task RunOnceAsync([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
    {
        try
        {
            var forecast = await weatherTransport.FetchForecastAsync(CancellationToken.None).ConfigureAwait(false);
            if (forecast is not null)
            {
                await logsTransport.UploadToLogsAsync(forecast, CancellationToken.None).ConfigureAwait(false);
            }

            logOk();
            
            if (myTimer.ScheduleStatus is not null)
            {
                logNextTimer(myTimer.ScheduleStatus.Next);
            }
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: OK", EventId = 1000)]
    public partial void logOk([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Location}: Next timer at {Moment}", EventId = 1001)]
    public partial void logNextTimer(DateTime Moment, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = null);        
}
