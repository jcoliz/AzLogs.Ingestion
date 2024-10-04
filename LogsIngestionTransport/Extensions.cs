using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.Options;
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
    public static IHostApplicationBuilder AddLogsIngestionTransport(this IHostApplicationBuilder builder)
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

            // Add an Azure credential to the client, using details from configuration

            // TODO: Is there not a better way to get client identity out of config??
            IdentityOptions idOptions = new();
            builder.Configuration.Bind(IdentityOptions.Section, idOptions);

            clientBuilder.UseCredential
            (
                new ClientSecretCredential
                (
                    tenantId: idOptions.TenantId.ToString(), 
                    clientId: idOptions.AppId.ToString(),
                    clientSecret: idOptions.AppSecret
                )
            );
            // NOTE: In production, we would simply use: `clientBuilder.UseCredential(new DefaultAzureCredential());`
            // which will use this application's managed identity (either as an App Service or Azure Function)
        });

        return builder;
    }
}
