namespace Guardian.Shared.Models;

public sealed class QuarantineRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Where the file originally lived (e.g. the Downloads folder) before it was quarantined.
    /// </summary>
    public string OriginalPath { get; init; } = string.Empty;

    /// <summary>
    /// Where Guardian moved the file to under GuardianPaths.QuarantineFilesDirectory.
    /// </summary>
    public string QuarantineFilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public int RiskScore { get; init; }

    public RiskLevel RiskLevel { get; init; }

    public DateTimeOffset QuarantinedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public QuarantineStatus Status { get; init; } = QuarantineStatus.Quarantined;
}
