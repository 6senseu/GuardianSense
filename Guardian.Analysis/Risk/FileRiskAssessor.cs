using Guardian.Shared.Models;

namespace Guardian.Analysis.Risk;

public sealed class FileRiskAssessor
{
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

    public LocalRiskAssessment Assess(
        string filePath,
        FileSignatureResult fileSignatureResult)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(fileSignatureResult);

        string fileName = Path.GetFileName(filePath);
        string extension = Path.GetExtension(fileName);

        List<string> reasons = new();

        int score;
        string category;

        if (string.IsNullOrWhiteSpace(extension))
        {
            score = 45;
            category = "unknown";

            reasons.Add(
                "Die Datei besitzt keine erkennbare Dateiendung.");
        }
        else if (VeryHighRiskExtensions.Contains(extension))
        {
            score = 90;
            category = "script-or-system-change";

            reasons.Add(
                $"Die Endung {extension} kann unmittelbar Befehle oder Skripte ausführen.");
        }
        else if (HighRiskExtensions.Contains(extension))
        {
            score = 75;
            category = "executable";

            reasons.Add(
                $"Die Endung {extension} gehört zu einem ausführbaren oder systemnahen Dateityp.");
        }
        else if (MediumRiskExtensions.Contains(extension))
        {
            score = 45;
            category = "document-or-archive";

            reasons.Add(
                $"Die Endung {extension} kann aktive Inhalte oder weitere Dateien enthalten.");
        }
        else if (LowRiskExtensions.Contains(extension))
        {
            score = 10;
            category = "data-file";

            reasons.Add(
                $"Die Endung {extension} gehört normalerweise zu einer Datendatei.");
        }
        else
        {
            score = 30;
            category = "unclassified";

            reasons.Add(
                $"Die Endung {extension} ist Guardian noch nicht eindeutig bekannt.");
        }

        if (HasSuspiciousDoubleExtension(fileName))
        {
            score += 25;

            reasons.Add(
                "Der Dateiname enthält eine mögliche doppelte Dateiendung.");
        }

        if (HasSignatureMismatch(fileSignatureResult))
        {
            int mismatchScore =
                GetSignatureMismatchRiskScore(
                    fileSignatureResult.DetectedType);

            score += mismatchScore;

            reasons.Add(
                $"Die Dateisignatur wurde als {fileSignatureResult.DetectedType} erkannt, passt aber nicht zur Dateiendung.");
        }

        score = Math.Clamp(score, 0, 100);

        return new LocalRiskAssessment
        {
            Score = score,
            Level = ConvertScoreToLevel(score),
            Category = category,
            Reasons = reasons
        };
    }

    private static bool HasSignatureMismatch(
        FileSignatureResult fileSignatureResult)
    {
        return !fileSignatureResult.MatchesExtension &&
               !string.Equals(
                   fileSignatureResult.DetectedType,
                   "unknown",
                   StringComparison.OrdinalIgnoreCase);
    }

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