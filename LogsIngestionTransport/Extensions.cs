using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.Options;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Hosting extensions for LogsIngestionTransport
/// </summary>
public static class LogsIngeestionTransportExtensions
{
    /// <summary>
    /// Add services for LogsIngestionTransport
    /// </summary>
    /// <param name="builder">Target to add</param>
    /// <returns>Same target returned</returns>
    public static IHostApplicationBuilder AddLogsIngestionTransport(this IHostApplicationBuilder builder, TokenCredential token)
    {
        builder.Services.Configure<LogIngestionOptions>(
            builder.Configuration.GetSection(LogIngestionOptions.Section)
        );

        builder.Services.AddTransient<LogsTransport>();

        builder.Services.AddAzureClients(clientBuilder => 
        {
            // Add a log ingestion client, using endpoint from configuration

            LogIngestionOptions logOptions = new();
            builder.Configuration.Bind(LogIngestionOptions.Section, logOptions);

            clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri);

            // Add the desired Azure credential to the client
            clientBuilder.UseCredential(token);
        });

        return builder;
    }
}
