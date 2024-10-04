namespace AzLogs.Ingestion.Options;

/// <summary>
/// Options for configuring the background worker service
/// </summary>
public record WorkerOptions
{
    /// <summary>
    /// Config file section
    /// </summary>
    public static readonly string Section = "Worker";

    public TimeSpan Frequency {
        get;
        init;
    } 
    = TimeSpan.FromSeconds(5);
}
