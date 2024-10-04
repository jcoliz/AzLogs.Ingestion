using System.Runtime.CompilerServices;
using AzLogs.Ingestion.Options;
using Azure.Monitor.Ingestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzLogs.Ingestion.LogsIngestionTransport;

public partial class LogsTransport(
    LogsIngestionClient logsClient,
    IOptions<LogIngestionOptions> logOptions,
    ILogger<LogsTransport> logger
) 
{
    /// <summary>
    /// Where to send application logs
    /// </summary>
    /// <seealso href="https://adamstorr.co.uk/blog/primary-constructor-and-logging-dont-mix/">
    private readonly ILogger<LogsTransport> _logger = logger;

    /// <summary>
    /// Send forecast up to Log Analytics
    /// </summary>
    /// <param name="period">Forecast data received from NWS</param>
    /// <param name="stoppingToken">Cancellation token</param>
    public async Task UploadToLogsAsync(object period, CancellationToken stoppingToken)
    {
        try
        {
            var response = await logsClient.UploadAsync
            (
                ruleId: logOptions.Value.DcrImmutableId, 
                streamName: logOptions.Value.Stream,
                logs: [ period ],
                cancellationToken: stoppingToken
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

    [LoggerMessage(Level = LogLevel.Information, Message = "{Location}: Sent OK {Status}", EventId = 1020)]
    public partial void logSentOk(int Status, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Send failed, returned no response", EventId = 1027)]
    public partial void logSendNoResponse([CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Send failed {Status}", EventId = 1028)]
    public partial void logSendFail(int Status, [CallerMemberName] string? location = null);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Location}: Failed", EventId = 1008)]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = null);
}