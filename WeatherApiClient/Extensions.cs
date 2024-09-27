using AzLogs.Ingestion.Api;
using AzLogs.Ingestion.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

public static class Extensions
{
    /// <summary>
    /// Add a weather client to the dependency injection contianer 
    /// </summary>
    /// <param name="builder">Existing application builder</param>
    /// <returns>Updated application builder</returns>
    public static IHostBuilder AddWeatherApiClient(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) => {
            services.Configure<WeatherOptions>(
                context.Configuration.GetSection(WeatherOptions.Section)
            );

            services.AddHttpClient<WeatherClient>();

        });
        return builder;
    }
}
