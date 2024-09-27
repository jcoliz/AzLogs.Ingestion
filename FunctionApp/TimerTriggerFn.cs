using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using AzLogs.Ingestion.Api;
using AzLogs.Ingestion.Options;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunctionApp
{
    public partial class TimerTriggerFn(
        WeatherClient weatherClient, 
        IOptions<WeatherOptions> weatherOptions, 
        ILogger<TimerTriggerFn> logger
    )
    {
        private readonly ILogger _logger = logger;

        [Function("TimerTriggerFn")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            _ = await FetchForecastAsync();
            
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

        [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Received OK {Result}", EventId = 1010)]
        public partial void logReceivedOk(string result, [CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Received malformed response", EventId = 1018)]
        public partial void logReceivedMalformed([CallerMemberName] string? location = null);

        [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
        public partial void logFail(Exception ex, [CallerMemberName] string? location = null);

    }
}
