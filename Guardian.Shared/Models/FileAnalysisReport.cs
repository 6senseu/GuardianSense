namespace Guardian.Shared.Models;

public sealed class FileAnalysisReport
{
    public Guid Id { get; init; } = Guid.NewGuid();

    // ------------------------------------------------------------------------
    // Analysis
    // ------------------------------------------------------------------------

    public DateTimeOffset DetectedAtUtc { get; init; }

    public string Status { get; init; } = "completed";

    // ------------------------------------------------------------------------
    // File Information
    // ------------------------------------------------------------------------

    public string FileName { get; init; } = string.Empty;

    public string OriginalPath { get; init; } = string.Empty;

    public string Extension { get; init; } = string.Empty;

    public long SizeBytes { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset LastModifiedAtUtc { get; init; }

    // ------------------------------------------------------------------------
    // File Identification
    // ------------------------------------------------------------------------

    public string Sha256 { get; init; } = string.Empty;

    public string DetectedFileType { get; init; } = string.Empty;

    public bool FileTypeMatchesExtension { get; init; }

    public string FileSignature { get; init; } = string.Empty;

    // ------------------------------------------------------------------------
    // Authenticode
    // ------------------------------------------------------------------------

    public bool IsSigned { get; init; }

    public bool IsSignatureValid { get; init; }

    public int AuthenticodeResult { get; init; }

    public SignatureStatus SignatureStatus { get; init; } =
        SignatureStatus.Unknown;

    public string? Publisher { get; init; }

    /// <summary>
    /// Name of the certificate authority that issued the signing certificate.
    /// </summary>
    public string? Issuer { get; init; }

    /// <summary>
    /// Start of the signing certificate's validity period.
    /// </summary>
    public DateTimeOffset? SignatureValidFrom { get; init; }

    /// <summary>
    /// End of the signing certificate's validity period.
    /// </summary>
    public DateTimeOffset? SignatureValidUntil { get; init; }

    // ------------------------------------------------------------------------
    // Cloud Reputation
    // ------------------------------------------------------------------------

    public CloudReputationStatus CloudReputationStatus { get; init; } =
        CloudReputationStatus.NotChecked;

    public int CloudMaliciousCount { get; init; }

    public int CloudSuspiciousCount { get; init; }

    public int CloudHarmlessCount { get; init; }

    public int CloudUndetectedCount { get; init; }

    public string? CloudPermalinkUrl { get; init; }

    // ------------------------------------------------------------------------
    // Risk
    // ------------------------------------------------------------------------

    public int LocalRiskScore { get; init; }

    public RiskLevel LocalRiskLevel { get; init; }

    public string LocalRiskCategory { get; init; } = string.Empty;

    public IReadOnlyList<string> LocalRiskReasons { get; init; } =
        Array.Empty<string>();

    // ------------------------------------------------------------------------
    // Metadata
    // ------------------------------------------------------------------------

    public string Source { get; init; } = "download-watcher";

    public string GuardianVersion { get; init; } = "0.3.0";
}