namespace Guardian.Shared.Models;

public sealed class FileSignatureResult
{
    public string DetectedType { get; init; } = "unknown";

    public bool MatchesExtension { get; init; }

    public string Signature { get; init; } = "";
}