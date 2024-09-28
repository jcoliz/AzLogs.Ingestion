using AzLogs.Ingestion.Api;
using AzLogs.Ingestion.Options;
using Microsoft.Extensions.DependencyInjection;
using Azure.Identity;
using Microsoft.Extensions.Azure;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    public static IHostBuilder ____AddAzureClientClients(this IHostBuilder builder)
    {
        //services.Configure<LogIngestionOptions>(
        //    context.Configuration.GetSection(LogIngestionOptions.Section)
        //);

        // Add a log ingestion client, using endpoint from configuration

        LogIngestionOptions logOptions = new();
        //context.Configuration.Bind(LogIngestionOptions.Section, logOptions);

        //builder.AddLogsIngestionClient(logOptions.EndpointUri);

        return builder;
    }
}
