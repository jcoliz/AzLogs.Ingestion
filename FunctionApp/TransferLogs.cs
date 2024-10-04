using System.Runtime.CompilerServices;
using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.WeatherApiClient;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FunctionApp
{
    public partial class TransferLogs(
        WeatherTransport weatherTransport, 
        LogsTransport logsTransport,
        ILogger<TransferLogs> _logger
    )
    {
        private readonly ILogger logger = _logger;

        [Function("TransferLogs")]
        public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer)
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

        [LoggerMessage(Level = LogLevel.Debug, Message = "{Location}: Next timer at {Moment}", EventId = 1008)]
        public partial void logNextTimer(DateTime Moment, [CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
        public partial void logFail(Exception ex, [CallerMemberName] string? location = null);        
    }
}
