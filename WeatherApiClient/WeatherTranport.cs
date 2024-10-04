using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzLogs.Ingestion.WeatherApiClient;

public partial class WeatherTransport(
        WeatherClient client,
        IOptions<WeatherOptions> options, 
        ILogger<WeatherTransport> logger)
{
    /// <summary>
    /// Where to send application logs
    /// </summary>
    /// <seealso href="https://adamstorr.co.uk/blog/primary-constructor-and-logging-dont-mix/">
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Fetch forecast from Weather Service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task<GridpointForecastPeriod?> FetchForecastAsync(CancellationToken stoppingToken)
    {
        GridpointForecastPeriod? result = default;

        try
        {
            var forecast = await client.Gridpoint_ForecastAsync(
                options.Value.Office, 
                options.Value.GridX, 
                options.Value.GridY, 
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
}
