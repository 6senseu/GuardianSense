namespace Guardian.Shared;

public sealed class GuardianSettings
{
    public const string SectionName = "Guardian";

    public string Language { get; set; } = "de-DE";

    public string DownloadDirectory { get; set; } = string.Empty;

    public bool MonitorSubdirectories { get; set; }

    public int FileReadyTimeoutSeconds { get; set; } = 30;
}