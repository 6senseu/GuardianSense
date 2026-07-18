using Guardian.Shared.Models;
using Guardian.Analysis.Authenticode;

namespace Guardian.Analysis.Pipeline;

public sealed class AnalysisContext
{
    public AnalysisContext(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        FilePath = Path.GetFullPath(filePath);
        StartedAtUtc = DateTimeOffset.UtcNow;
    }

    public string FilePath { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public FileSignatureResult Signature { get; set; } = new();

    public AuthenticodeResult Authenticode { get; set; } = new();

    public LocalRiskAssessment Risk { get; set; } =
        new();

    public List<string> Errors { get; } =
        new();

    public bool HasErrors =>
        Errors.Count > 0;
}