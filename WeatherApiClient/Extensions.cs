using AzLogs.Ingestion.WeatherApiClient;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Hosting extensions for WeatherTransport
/// </summary>
public static class WeatherTransportExtensions
{
    /// <summary>
    /// Add WeatherTransport services to the dependency injection container
    /// </summary>
    /// <param name="builder">Existing host builder</param>
    /// <returns>Updated host builder</returns>
    public static IHostBuilder AddWeatherTransport(this IHostBuilder builder)
    {
        builder.ConfigureServices((context, services) => {
            services.Configure<WeatherOptions>(
                context.Configuration.GetSection(WeatherOptions.Section)
            );

            services.AddHttpClient<WeatherClient>();
            services.AddTransient<WeatherTransport>();

        });
        return builder;
    }    
}