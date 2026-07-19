namespace Guardian.Shared.Models;

public sealed class CloudReputationResult
{
    public CloudReputationStatus Status { get; init; } = CloudReputationStatus.NotChecked;

    public int MaliciousCount { get; init; }

    public int SuspiciousCount { get; init; }

    public int HarmlessCount { get; init; }

    public int UndetectedCount { get; init; }

    public string? PermalinkUrl { get; init; }
}
