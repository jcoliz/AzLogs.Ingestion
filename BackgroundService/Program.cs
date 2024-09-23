using Azure.Identity;
using Microsoft.Extensions.Azure;
using Weather.Worker;
using Weather.Worker.Api;
using Weather.Worker.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);

builder.Services.Configure<WeatherOptions>(
    builder.Configuration.GetSection("Weather")
);
builder.Services.Configure<IdentityOptions>(
    builder.Configuration.GetSection(IdentityOptions.Section)
);

builder.Services.AddHttpClient<WeatherClient>();
builder.Services.AddHostedService<Worker>();
builder.Services.AddAzureClients(clientBuilder => 
{
    // TODO: Is there not a better way to get client identity out of config??
    IdentityOptions options = new();
    builder.Configuration.Bind(IdentityOptions.Section, options);

    clientBuilder.UseCredential
    (
        new ClientSecretCredential
        (
            options.TenantId.ToString(), 
            options.AppId.ToString(),
            options.AppSecret
        )
    );
});

var host = builder.Build();

await host.RunAsync();