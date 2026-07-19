using Guardian.Shared;
using Guardian.Shared.Models;
using Guardian.Shared.Storage;

namespace Guardian.Service;

/// <summary>
/// Decides whether an analyzed file should be quarantined and, if so,
/// moves it out of reach before the user could ever run it.
/// </summary>
public sealed class QuarantineManager
{
    private readonly ILogger<QuarantineManager> _logger;
    private readonly QuarantineStore _quarantineStore;

    public QuarantineManager(
        ILogger<QuarantineManager> logger,
        QuarantineStore quarantineStore)
    {
        _logger = logger;
        _quarantineStore = quarantineStore;
    }

    /// <summary>
    /// Quarantines the analyzed file when its risk level is high enough.
    /// Returns the created record, or null when no action was taken.
    /// </summary>
    public async Task<QuarantineRecord?> HandleAsync(
        FileAnalysisReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.LocalRiskLevel != RiskLevel.VeryHigh)
        {
            return null;
        }

        if (!File.Exists(report.OriginalPath))
        {
            _logger.LogWarning(
                "File to quarantine no longer exists: {Path}",
                report.OriginalPath);

            return null;
        }

        Directory.CreateDirectory(GuardianPaths.QuarantineFilesDirectory);

        Guid recordId = Guid.NewGuid();
        string quarantineFilePath = Path.Combine(
            GuardianPaths.QuarantineFilesDirectory,
            $"{recordId:N}_{report.FileName}");

        try
        {
            File.Move(report.OriginalPath, quarantineFilePath);
        }
        catch (IOException exception)
        {
            _logger.LogError(
                exception,
                "Could not move file to quarantine: {Path}",
                report.OriginalPath);

            return null;
        }

        QuarantineRecord record = new()
        {
            Id = recordId,
            OriginalPath = report.OriginalPath,
            QuarantineFilePath = quarantineFilePath,
            FileName = report.FileName,
            Sha256 = report.Sha256,
            Reason = BuildReason(report),
            RiskScore = report.LocalRiskScore,
            RiskLevel = report.LocalRiskLevel,
            QuarantinedAtUtc = DateTimeOffset.UtcNow,
            Status = QuarantineStatus.Quarantined
        };

        await _quarantineStore.SaveAsync(record, cancellationToken);

        _logger.LogWarning(
            "Quarantined {FileName} (risk score {RiskScore}/100): {Reason}",
            record.FileName,
            record.RiskScore,
            record.Reason);

        return record;
    }

    private static string BuildReason(FileAnalysisReport report)
    {
        return report.LocalRiskReasons.Count > 0
            ? string.Join(" ", report.LocalRiskReasons)
            : $"Local risk score {report.LocalRiskScore}/100 ({report.LocalRiskLevel}).";
    }
}
