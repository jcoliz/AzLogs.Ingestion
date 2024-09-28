using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AzLogs.Ingestion.Api;
using AzLogs.Ingestion.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Monitor.Ingestion;

namespace FunctionApp
{
    public partial class TimerTriggerFn(
        WeatherClient weatherClient, 
        LogsIngestionClient logsClient,
        IOptions<WeatherOptions> weatherOptions, 
        IOptions<LogIngestionOptions> logOptions,
        ILogger<TimerTriggerFn> logger
    )
    {
        private readonly ILogger _logger = logger;

        [Function("TimerTriggerFn")]
        public async Task Run([TimerTrigger("*/30 * * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var forecast = await FetchForecastAsync();
            if (forecast is not null)
            {
                await UploadToLogsAsync(forecast).ConfigureAwait(false);
            }
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }

        /// <summary>
        /// Fetch forecast from Weather Service
        /// </summary>
        /// <param name="stoppingToken">Cancellation token</param>
        private async Task<GridpointForecastPeriod?> FetchForecastAsync()
        {
            GridpointForecastPeriod? result = default;

            try
            {
                var forecast = await weatherClient.Gridpoint_ForecastAsync(
                    weatherOptions.Value.Office, 
                    weatherOptions.Value.GridX, 
                    weatherOptions.Value.GridY
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

        /// <summary>
        /// Send forecast up to Log Analytics
        /// </summary>
        /// <param name="period">Forecast data received from NWS</param>
        /// <param name="stoppingToken">Cancellation token</param>
        private async Task UploadToLogsAsync(GridpointForecastPeriod period)
        {
            try
            {
                var response = await logsClient.UploadAsync
                (
                    ruleId: logOptions.Value.DcrImmutableId, 
                    streamName: logOptions.Value.Stream,
                    logs: [ period ]
                )
                .ConfigureAwait(false);

                switch (response?.IsError)
                {
                    case null:
                        logSendNoResponse();
                        break;

                    case true:
                        logSendFail(response.Status);
                        break;

                    default:
                        logSentOk(response.Status);
                        break;
                }
            }
            catch (Exception ex)
            {
                logFail(ex);
            }
        }

        [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Received OK {Result}", EventId = 1010)]
        public partial void logReceivedOk(string result, [CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Received malformed response", EventId = 1018)]
        public partial void logReceivedMalformed([CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Sent OK {Status}", EventId = 1020)]
        public partial void logSentOk(int Status, [CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Send failed, returned no response", EventId = 1027)]
        public partial void logSendNoResponse([CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Send failed {Status}", EventId = 1028)]
        public partial void logSendFail(int Status, [CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
        public partial void logFail(Exception ex, [CallerMemberName] string? location = null);

    }
}
