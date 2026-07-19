using System.Text.Json;
using System.Text.Json.Serialization;

namespace Guardian.Shared.Storage;

/// <summary>
/// Shared JSON format for everything Guardian persists under %ProgramData%\Guardian
/// (reports, quarantine records). Kept in one place because the Service (writer) and
/// the Dashboard (reader) run as separate processes and must agree on the exact format.
/// </summary>
public static class GuardianJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
}
