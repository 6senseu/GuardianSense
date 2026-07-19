namespace Guardian.Shared.Models;

public enum CloudReputationStatus
{
    /// <summary>No cloud check was performed (disabled, no API key, or no hash yet).</summary>
    NotChecked = 0,

    /// <summary>No security vendor flagged the file.</summary>
    Clean = 1,

    /// <summary>At least one security vendor flagged the file as suspicious.</summary>
    Suspicious = 2,

    /// <summary>At least one security vendor flagged the file as malicious.</summary>
    Malicious = 3,

    /// <summary>The hash is not known to VirusTotal yet.</summary>
    NotFound = 4,

    /// <summary>The check could not be completed (network error, rate limit, etc.).</summary>
    Unavailable = 5
}
