namespace Common.Configurations;

public sealed class DatabaseReadinessCfg
{
    #region Constants

    public const string Section = "DatabaseReadiness";

    public const string Enable = "Enable";

    /// <summary>How many connection attempts the startup gate makes before giving up.</summary>
    public const string MaxAttempts = "MaxAttempts";

    /// <summary>Delay before the first retry, in seconds. Later retries use exponential backoff on this value.</summary>
    public const string BaseDelaySeconds = "BaseDelaySeconds";

    /// <summary>Upper bound for a single retry delay, in seconds.</summary>
    public const string MaxDelaySeconds = "MaxDelaySeconds";

    #endregion
}
