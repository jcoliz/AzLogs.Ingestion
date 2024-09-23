using Azure.Identity;
using Microsoft.Extensions.Azure;
using Weather.Worker;
using Weather.Worker.Api;
using Weather.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

builder.Services.Configure<IdentityOptions>(
    builder.Configuration.GetSection(IdentityOptions.Section)
);
builder.Services.Configure<WeatherOptions>(
    builder.Configuration.GetSection("Weather")
);
builder.Services.Configure<LogIngestionOptions>(
    builder.Configuration.GetSection(LogIngestionOptions.Section)
);

builder.Services.AddHttpClient<WeatherClient>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddAzureClients(clientBuilder => 
{
    // Add a log ingestion client, using endpoint from configuration

    LogIngestionOptions logOptions = new();
    builder.Configuration.Bind(LogIngestionOptions.Section, logOptions);

    clientBuilder.AddLogsIngestionClient(logOptions.Endpoint);

    // Add an Azure credential to the client, using details from configuration

    // TODO: Is there not a better way to get client identity out of config??
    IdentityOptions idOptions = new();
    builder.Configuration.Bind(IdentityOptions.Section, idOptions);

    clientBuilder.UseCredential
    (
        new ClientSecretCredential
        (
            idOptions.TenantId.ToString(), 
            idOptions.AppId.ToString(),
            idOptions.AppSecret
        )
    );
    // NOTE: In production, we would simply use: `clientBuilder.UseCredential(new DefaultAzureCredential());`
    // which will use this application's managed identity (either as an App Service or Azure Function)
});

var host = builder.Build();

await host.RunAsync();