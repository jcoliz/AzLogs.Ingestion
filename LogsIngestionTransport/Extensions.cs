using AzLogs.Ingestion.LogsIngestionTransport;
using AzLogs.Ingestion.Options;
using Azure.Core;
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

    /// <summary>
    /// Add services for LogsIngestionTransport
    /// </summary>
    /// <param name="builder">Existing application builder</param>
    /// <param name="token">Token to use for long ingestion</param>
    /// <returns>Updated application builder</returns>
    public static IHostBuilder AddLogsIngestionTransport(this IHostBuilder builder, TokenCredential token)
    {
        return builder.ConfigureServices((context, services) => 
            ConfigureServices(context, services, token)
        );
    }

    /// <summary>
    /// Add services for LogsIngestionTransport
    /// </summary>
    /// <param name="builder">Existing application builder</param>
    /// <param name="getTokenFunc">Function to get a Token to use for long ingestion</param>
    /// <returns>Updated application builder</returns>
    public static IHostBuilder AddLogsIngestionTransport(this IHostBuilder builder, Func<HostBuilderContext,TokenCredential> getTokenFunc)
    {
        return builder.ConfigureServices((context, services) => 
            ConfigureServices(context, services, getTokenFunc(context))
        );
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services, TokenCredential token)
    {
        services.Configure<LogIngestionOptions>(
            context.Configuration.GetSection(LogIngestionOptions.Section)
        );

        services.AddTransient<LogsTransport>();

        services.AddAzureClients(clientBuilder =>
        {
            // Add a log ingestion client, using endpoint from configuration

            LogIngestionOptions logOptions = new();
            context.Configuration.Bind(LogIngestionOptions.Section, logOptions);

            clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri);

            // Add the desired Azure credential to the client
            clientBuilder.UseCredential(token);
        });
    }
}
