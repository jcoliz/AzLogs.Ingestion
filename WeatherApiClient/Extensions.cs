using AzLogs.Ingestion.WeatherApiClient;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Hosting extensions for WeatherApiClient
/// </summary>
public static class WeatherApiClientExtensions
{
    /// <summary>
    /// Add services for WeatherApiClient
    /// </summary>
    /// <param name="builder">Target to add</param>
    /// <returns>Same target returned</returns>
    public static IHostApplicationBuilder AddWeatherApiClient(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<WeatherOptions>(
            builder.Configuration.GetSection(WeatherOptions.Section)
        );

        builder.Services.AddHttpClient<WeatherClient>();
        builder.Services.AddTransient<WeatherTransport>();

        return builder;
    }
}