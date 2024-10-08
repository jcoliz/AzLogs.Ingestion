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

// Set up weather client and transport
builder.AddWeatherTransport();

// Set up logs transport with Azure identity as specified in config
IdentityOptions idOptions = new();
builder.Configuration.Bind(IdentityOptions.Section, idOptions);
builder.AddLogsIngestionTransport(
    // NOTE: In production, we would simply use `new DefaultAzureCredential()`
    // which will use this application's managed identity (either as an App Service or Azure Function)
    new ClientSecretCredential
    (
        tenantId: idOptions.TenantId.ToString(), 
        clientId: idOptions.AppId.ToString(),
        clientSecret: idOptions.AppSecret
    )
);

// Off we go!
var host = builder.Build();
await host.RunAsync();
