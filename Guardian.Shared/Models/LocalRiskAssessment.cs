namespace Guardian.Shared.Models;

public sealed class LocalRiskAssessment
{
    public RiskLevel Level { get; init; }

    public int Score { get; init; }

    public string Category { get; init; } = string.Empty;

    public IReadOnlyList<string> Reasons { get; init; } =
        Array.Empty<string>();
}