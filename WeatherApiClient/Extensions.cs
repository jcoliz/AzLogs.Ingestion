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
    /// <param name="builder">Target to add</param>
    /// <returns>Same target returned</returns>
    public static IHostApplicationBuilder AddWeatherTransport(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WeatherOptions>(
            builder.Configuration.GetSection(WeatherOptions.Section)
        );

        builder.Services.AddHttpClient<WeatherClient>();
        builder.Services.AddTransient<WeatherTransport>();

        return builder;
    }

    /// <summary>
    /// Add WeatherTransport services to the dependency injection container
    /// </summary>
    /// <param name="builder">Existing application builder</param>
    /// <returns>Updated application builder</returns>
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