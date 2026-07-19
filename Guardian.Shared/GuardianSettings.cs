namespace Guardian.Shared;

public sealed class GuardianSettings
{
    public const string SectionName = "Guardian";

    public string Language { get; set; } = "en-US";

    public string DownloadDirectory { get; set; } = string.Empty;

    public bool MonitorSubdirectories { get; set; }

    public int FileReadyTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Free VirusTotal API key (see virustotal.com). Cloud reputation checks are
    /// skipped when this is empty, even if CloudReputationEnabled is true.
    /// </summary>
    public string VirusTotalApiKey { get; set; } = string.Empty;

    public bool CloudReputationEnabled { get; set; }
}