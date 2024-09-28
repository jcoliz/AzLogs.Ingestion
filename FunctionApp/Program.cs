using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AzLogs.Ingestion.Options;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Microsoft.Extensions.Azure;
using Azure.Monitor.Ingestion;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context,services) => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get log ingestion options from app configuration
        services.Configure<LogIngestionOptions>(
            context.Configuration.GetSection(LogIngestionOptions.Section)
        );

        // Add a Log Ingestion Client to connect with Azure
        services.AddAzureClients(clientBuilder => 
        {
            LogIngestionOptions logOptions = new();
            context.Configuration.Bind(LogIngestionOptions.Section, logOptions);

            clientBuilder.AddLogsIngestionClient(logOptions.EndpointUri);
            clientBuilder.UseCredential(new DefaultAzureCredential());
        });
    })
    .AddWeatherApiClient()
    .Build();

host.Run();
