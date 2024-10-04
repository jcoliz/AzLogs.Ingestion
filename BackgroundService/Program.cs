using AzLogs.Ingestion;
using AzLogs.Ingestion.Options;
using Azure.Identity;
using Microsoft.Extensions.Azure;

var builder = Host.CreateApplicationBuilder(args);

// Optional source of local config secrets
builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

// Set up the worker service
builder.Services.Configure<WorkerOptions>(
    builder.Configuration.GetSection(WorkerOptions.Section)
);
builder.Services.AddHostedService<Worker>();

// Set up the services we depend on
builder.AddWeatherApiClient();
builder.AddLogsIngestionTransport();

// And off we go!
var host = builder.Build();
await host.RunAsync();
