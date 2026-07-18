namespace Guardian.Shared.Models;

public sealed class FileAnalysisReport
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTimeOffset DetectedAtUtc { get; init; }

    public string Status { get; init; } = "completed";

    public string FileName { get; init; } = string.Empty;

    public string OriginalPath { get; init; } = string.Empty;

    public string Extension { get; init; } = string.Empty;

    public string DetectedFileType { get; init; } = string.Empty;

    public bool FileTypeMatchesExtension { get; init; }

    public string FileSignature { get; init; } = string.Empty;

    public string Sha256 { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset LastModifiedAtUtc { get; init; }

    public int LocalRiskScore { get; init; }

    public RiskLevel LocalRiskLevel { get; init; }

    public string LocalRiskCategory { get; init; } = string.Empty;

    public IReadOnlyList<string> LocalRiskReasons { get; init; } =
        Array.Empty<string>();

    public string Source { get; init; } = "download-watcher";

    public string GuardianVersion { get; init; } = "0.2.4";
}