using Guardian.Analysis.Authenticode;
using Guardian.Analysis.Risk;
using Guardian.Shared.Models;

namespace Guardian.Tests.Risk;

public sealed class FileRiskAssessorTests
{
    private readonly FileRiskAssessor _assessor = new();

    [Fact]
    public void Assess_LowRiskExtension_ReturnsLowScore()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "jpg",
            Signature = "FFD8FF",
            MatchesExtension = true
        };

        LocalRiskAssessment result = _assessor.Assess(
            @"C:\Downloads\photo.jpg",
            signature);

        Assert.True(result.Score <= 35);
        Assert.True(result.Level is RiskLevel.VeryLow or RiskLevel.Low);
    }

    [Fact]
    public void Assess_ScriptExtension_ReturnsHighScore()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "unknown",
            Signature = "",
            MatchesExtension = true
        };

        LocalRiskAssessment result = _assessor.Assess(
            @"C:\Downloads\install.ps1",
            signature);

        Assert.True(result.Score >= 80);
        Assert.Equal(RiskLevel.VeryHigh, result.Level);
    }

    [Fact]
    public void Assess_SuspiciousDoubleExtension_IncreasesScore()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = true
        };

        LocalRiskAssessment withoutDoubleExtension = _assessor.Assess(
            @"C:\Downloads\setup.exe",
            signature);

        LocalRiskAssessment withDoubleExtension = _assessor.Assess(
            @"C:\Downloads\invoice.pdf.exe",
            signature);

        Assert.True(withDoubleExtension.Score > withoutDoubleExtension.Score);
    }

    [Fact]
    public void Assess_SignatureMismatch_IncreasesScore()
    {
        FileSignatureResult matching = new()
        {
            DetectedType = "pdf",
            Signature = "25504446",
            MatchesExtension = true
        };

        FileSignatureResult mismatched = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = false
        };

        LocalRiskAssessment matchingResult = _assessor.Assess(
            @"C:\Downloads\report.pdf",
            matching);

        LocalRiskAssessment mismatchedResult = _assessor.Assess(
            @"C:\Downloads\report.pdf",
            mismatched);

        Assert.True(mismatchedResult.Score > matchingResult.Score);
    }

    [Fact]
    public void Assess_ValidAuthenticodeSignature_ReducesScore()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = true
        };

        AuthenticodeResult validSignature = new()
        {
            IsSigned = true,
            IsValid = true,
            SignatureStatus = SignatureStatus.Valid
        };

        LocalRiskAssessment withoutAuthenticode = _assessor.Assess(
            @"C:\Downloads\tool.exe",
            signature);

        LocalRiskAssessment withValidSignature = _assessor.Assess(
            @"C:\Downloads\tool.exe",
            signature,
            validSignature);

        Assert.True(withValidSignature.Score < withoutAuthenticode.Score);
    }

    [Fact]
    public void Assess_RevokedAuthenticodeSignature_IncreasesScoreSignificantly()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = true
        };

        AuthenticodeResult revoked = new()
        {
            IsSigned = true,
            IsValid = false,
            SignatureStatus = SignatureStatus.Revoked
        };

        LocalRiskAssessment result = _assessor.Assess(
            @"C:\Downloads\tool.exe",
            signature,
            revoked);

        Assert.Equal(RiskLevel.VeryHigh, result.Level);
    }

    [Fact]
    public void Assess_VirusTotalConfirmsMalicious_ForcesVeryHighRiskEvenForLowRiskExtension()
    {
        // A .jpg would normally score very low - but VirusTotal knowing it's
        // malicious must override that local heuristic.
        FileSignatureResult signature = new()
        {
            DetectedType = "jpg",
            Signature = "FFD8FF",
            MatchesExtension = true
        };

        CloudReputationResult malicious = new()
        {
            Status = CloudReputationStatus.Malicious,
            MaliciousCount = 40
        };

        LocalRiskAssessment result = _assessor.Assess(
            @"C:\Downloads\photo.jpg",
            signature,
            authenticodeResult: null,
            cloudReputationResult: malicious);

        Assert.Equal(RiskLevel.VeryHigh, result.Level);
    }

    [Fact]
    public void Assess_VirusTotalCleanResult_ReducesScore()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = true
        };

        CloudReputationResult clean = new()
        {
            Status = CloudReputationStatus.Clean,
            HarmlessCount = 60
        };

        LocalRiskAssessment withoutCloudCheck = _assessor.Assess(
            @"C:\Downloads\tool.exe",
            signature);

        LocalRiskAssessment withCleanResult = _assessor.Assess(
            @"C:\Downloads\tool.exe",
            signature,
            authenticodeResult: null,
            cloudReputationResult: clean);

        Assert.True(withCleanResult.Score < withoutCloudCheck.Score);
    }

    [Fact]
    public void Assess_ScoreIsNeverOutsideZeroToHundredRange()
    {
        FileSignatureResult signature = new()
        {
            DetectedType = "exe",
            Signature = "4D5A",
            MatchesExtension = false
        };

        AuthenticodeResult revoked = new()
        {
            IsSigned = true,
            IsValid = false,
            SignatureStatus = SignatureStatus.Revoked
        };

        LocalRiskAssessment result = _assessor.Assess(
            @"C:\Downloads\invoice.pdf.exe",
            signature,
            revoked);

        Assert.InRange(result.Score, 0, 100);
    }
}
