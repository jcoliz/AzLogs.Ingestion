namespace AzLogs.Ingestion.WeatherApiClient;

/// <summary>
/// Options for calling into Weather Service
/// </summary>
public record WeatherOptions
{
    /// <summary>
    /// Config file section
    /// </summary>
    public static readonly string Section = "Weather";

    /// <summary>
    /// Which Weather Service office to check
    /// </summary>
    public NWSForecastOfficeId Office {
        get;
        init;
    }
    = NWSForecastOfficeId.ABQ;

    /// <summary>
    /// Grid position X coordinate
    /// </summary>
    public int GridX {
        get;
        init;
    }

    /// <summary>
    /// Grid position Y coordinate
    /// </summary>
    public int GridY {
        get;
        init;
    }

    public TimeSpan Frequency {
        get;
        init;
    } 
    = TimeSpan.FromSeconds(5);
}
