using Guardian.Analysis.Authenticode;
using Guardian.Shared.Models;

namespace Guardian.Analysis.Risk;

public sealed class FileRiskAssessor
{
    // File types that can directly run scripts or change the system.
    private static readonly HashSet<string> VeryHighRiskExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".ps1",
            ".psm1",
            ".bat",
            ".cmd",
            ".vbs",
            ".vbe",
            ".js",
            ".jse",
            ".wsf",
            ".wsh",
            ".scr",
            ".hta",
            ".reg"
        };

    // Executable and system-related file types.
    private static readonly HashSet<string> HighRiskExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".dll",
            ".msi",
            ".msp",
            ".com",
            ".cpl",
            ".sys",
            ".jar",
            ".lnk",
            ".appx",
            ".msix"
        };

    // Documents and archives that may contain active content.
    private static readonly HashSet<string> MediumRiskExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip",
            ".rar",
            ".7z",
            ".iso",
            ".img",
            ".pdf",
            ".doc",
            ".docx",
            ".docm",
            ".xls",
            ".xlsx",
            ".xlsm",
            ".ppt",
            ".pptx",
            ".pptm",
            ".rtf"
        };

    // Common data and media file types.
    private static readonly HashSet<string> LowRiskExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".gif",
            ".webp",
            ".bmp",
            ".mp3",
            ".wav",
            ".mp4",
            ".mkv",
            ".txt",
            ".csv"
        };

    /// <summary>
    /// Assesses a file without Authenticode information.
    /// </summary>
    public LocalRiskAssessment Assess(
        string filePath,
        FileSignatureResult fileSignatureResult)
    {
        return Assess(
            filePath,
            fileSignatureResult,
            authenticodeResult: null);
    }

    /// <summary>
    /// Assesses a file using its extension, file signature
    /// and optional Authenticode and cloud reputation information.
    /// </summary>
    public LocalRiskAssessment Assess(
        string filePath,
        FileSignatureResult fileSignatureResult,
        AuthenticodeResult? authenticodeResult,
        CloudReputationResult? cloudReputationResult = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(fileSignatureResult);

        string fileName = Path.GetFileName(filePath);
        string extension = Path.GetExtension(fileName);

        List<string> reasons = new();

        int score;
        string category;

        // Set the base score from the file extension.
        if (string.IsNullOrWhiteSpace(extension))
        {
            score = 45;
            category = "unknown";

            reasons.Add(
                "The file has no recognized extension.");
        }
        else if (VeryHighRiskExtensions.Contains(extension))
        {
            score = 90;
            category = "script-or-system-change";

            reasons.Add(
                $"The {extension} extension can directly run commands or scripts.");
        }
        else if (HighRiskExtensions.Contains(extension))
        {
            score = 75;
            category = "executable";

            reasons.Add(
                $"The {extension} extension belongs to an executable or system-related file type.");
        }
        else if (MediumRiskExtensions.Contains(extension))
        {
            score = 45;
            category = "document-or-archive";

            reasons.Add(
                $"The {extension} extension may contain active content or other files.");
        }
        else if (LowRiskExtensions.Contains(extension))
        {
            score = 10;
            category = "data-file";

            reasons.Add(
                $"The {extension} extension normally belongs to a data file.");
        }
        else
        {
            score = 30;
            category = "unclassified";

            reasons.Add(
                $"Guardian does not clearly recognize the {extension} extension.");
        }

        // Detect names such as image.jpg.exe.
        if (HasSuspiciousDoubleExtension(fileName))
        {
            score += 25;

            reasons.Add(
                "The file name contains a suspicious double extension.");
        }

        // Check whether the real file type matches the extension.
        if (HasSignatureMismatch(fileSignatureResult))
        {
            int mismatchScore =
                GetSignatureMismatchRiskScore(
                    fileSignatureResult.DetectedType);

            score += mismatchScore;

            reasons.Add(
                $"The file signature was detected as {fileSignatureResult.DetectedType}, but it does not match the extension.");
        }

        // Apply a small score change based on Authenticode.
        ApplyAuthenticodeRisk(
            authenticodeResult,
            extension,
            ref score,
            reasons);

        // Cloud reputation is the strongest signal: a known-malicious hash
        // overrides local heuristics almost entirely.
        ApplyCloudReputationRisk(
            cloudReputationResult,
            ref score,
            reasons);

        score = Math.Clamp(score, 0, 100);

        return new LocalRiskAssessment
        {
            Score = score,
            Level = ConvertScoreToLevel(score),
            Category = category,
            Reasons = reasons
        };
    }

    /// <summary>
    /// Applies a conservative score change based on Authenticode.
    /// A valid signature does not automatically mean that a file is safe.
    /// </summary>
    private static void ApplyAuthenticodeRisk(
        AuthenticodeResult? authenticodeResult,
        string extension,
        ref int score,
        List<string> reasons)
    {
        // Do nothing if Authenticode data is not available.
        if (authenticodeResult is null)
        {
            return;
        }

        switch (authenticodeResult.SignatureStatus)
        {
            case SignatureStatus.Valid:
                score -= 10;

                reasons.Add(
                    "A valid Authenticode signature slightly reduces the local risk.");
                break;

            case SignatureStatus.Invalid:
                score += 30;

                reasons.Add(
                    "The Authenticode signature is invalid or no longer matches the file.");
                break;

            case SignatureStatus.Revoked:
                score += 40;

                reasons.Add(
                    "The signing certificate was revoked.");
                break;

            case SignatureStatus.Expired:
                score += 15;

                reasons.Add(
                    "The signature could not be confirmed because the certificate expired.");
                break;

            case SignatureStatus.NotSigned:
                // Missing signatures are mainly relevant for executable files.
                if (VeryHighRiskExtensions.Contains(extension) ||
                    HighRiskExtensions.Contains(extension))
                {
                    score += 10;

                    reasons.Add(
                        "The executable or script file has no embedded Authenticode signature.");
                }

                break;

            case SignatureStatus.Unknown:
            default:
                reasons.Add(
                    "The Authenticode status could not be clearly evaluated.");
                break;
        }
    }

    /// <summary>
    /// Applies a score change based on VirusTotal's verdict. A confirmed
    /// malicious hash forces the risk into VeryHigh regardless of any local
    /// heuristics, since this is the strongest signal Guardian has access to.
    /// </summary>
    private static void ApplyCloudReputationRisk(
        CloudReputationResult? cloudReputationResult,
        ref int score,
        List<string> reasons)
    {
        if (cloudReputationResult is null)
        {
            return;
        }

        switch (cloudReputationResult.Status)
        {
            case CloudReputationStatus.Malicious when cloudReputationResult.MaliciousCount >= 3:
                score = Math.Max(score, 95);

                reasons.Add(
                    $"VirusTotal flagged this file as malicious ({cloudReputationResult.MaliciousCount} security vendors).");
                break;

            case CloudReputationStatus.Malicious:
                score += 35;

                reasons.Add(
                    $"VirusTotal flagged this file as malicious ({cloudReputationResult.MaliciousCount} security vendor(s)).");
                break;

            case CloudReputationStatus.Suspicious:
                score += 20;

                reasons.Add(
                    $"VirusTotal flagged this file as suspicious ({cloudReputationResult.SuspiciousCount} security vendor(s)).");
                break;

            case CloudReputationStatus.Clean:
                score -= 10;

                reasons.Add(
                    "VirusTotal found no detections for this file.");
                break;

            case CloudReputationStatus.NotFound:
            case CloudReputationStatus.NotChecked:
            case CloudReputationStatus.Unavailable:
            default:
                // Hash unknown to VirusTotal, or the check was skipped/failed - no adjustment.
                break;
        }
    }

    /// <summary>
    /// Checks whether the detected file type matches the extension.
    /// </summary>
    private static bool HasSignatureMismatch(
        FileSignatureResult fileSignatureResult)
    {
        return !fileSignatureResult.MatchesExtension &&
               !string.Equals(
                   fileSignatureResult.DetectedType,
                   "unknown",
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the score increase for a file type mismatch.
    /// </summary>
    private static int GetSignatureMismatchRiskScore(
        string detectedType)
    {
        return detectedType.ToLowerInvariant() switch
        {
            "exe" => 80,
            "zip" => 35,
            "pdf" => 25,
            _ => 40
        };
    }

    /// <summary>
    /// Checks for file names such as invoice.pdf.exe.
    /// </summary>
    private static bool HasSuspiciousDoubleExtension(
        string fileName)
    {
        string[] parts = fileName.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3)
        {
            return false;
        }

        string finalExtension =
            "." + parts[^1];

        if (!VeryHighRiskExtensions.Contains(finalExtension) &&
            !HighRiskExtensions.Contains(finalExtension))
        {
            return false;
        }

        string previousExtension =
            "." + parts[^2];

        return LowRiskExtensions.Contains(previousExtension) ||
               MediumRiskExtensions.Contains(previousExtension);
    }

    /// <summary>
    /// Converts the numeric score into a risk level.
    /// </summary>
    private static RiskLevel ConvertScoreToLevel(int score)
    {
        return score switch
        {
            <= 15 => RiskLevel.VeryLow,
            <= 35 => RiskLevel.Low,
            <= 55 => RiskLevel.Medium,
            <= 80 => RiskLevel.High,
            _ => RiskLevel.VeryHigh
        };
    }
}