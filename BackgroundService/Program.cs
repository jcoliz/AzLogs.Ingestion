using AzLogs.Ingestion;
using AzLogs.Ingestion.Options;
using Azure.Identity;

var host = new HostBuilder()
    .ConfigureDefaults(args)
    
    .ConfigureAppConfiguration((appConfiguration) => {
        // Optional source of local config secrets
        appConfiguration.AddTomlFile("config.toml", optional: true, reloadOnChange: true);
    })
    
    .ConfigureServices((context, services) => {
        // Set up the worker service
        services.Configure<WorkerOptions>(
            context.Configuration.GetSection(WorkerOptions.Section)
        );
        services.AddHostedService<Worker>();
    })
    
    // Set up weather client and transport
    .AddWeatherTransport()

    // Set up logs transport with Azure identity as specified in config
    .AddLogsIngestionTransport(context => {

        // Return a client secret credential using options as described in config
        IdentityOptions idOptions = new();
        context.Configuration.Bind(IdentityOptions.Section, idOptions);
        
        // NOTE: In production, we would simply use `new DefaultAzureCredential()`
        // which will use this application's managed identity (either as an App Service or Azure Function)
        return new ClientSecretCredential
        (
            tenantId: idOptions.TenantId.ToString(),
            clientId: idOptions.AppId.ToString(),
            clientSecret: idOptions.AppSecret
        );

    })
    .Build();

await host.RunAsync();
