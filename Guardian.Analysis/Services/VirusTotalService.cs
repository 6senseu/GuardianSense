using System.Net;
using System.Text.Json;
using Guardian.Shared;
using Guardian.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Guardian.Analysis.Services;

/// <summary>
/// Looks up a file's SHA-256 hash against VirusTotal's known-malware database.
/// Requires a free API key (virustotal.com); the check is skipped cleanly when
/// no key is configured or cloud reputation is disabled.
/// </summary>
public sealed class VirusTotalService
{
    private readonly HttpClient _httpClient;
    private readonly GuardianSettings _settings;
    private readonly ILogger<VirusTotalService> _logger;

    public VirusTotalService(
        HttpClient httpClient,
        IOptions<GuardianSettings> options,
        ILogger<VirusTotalService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    private bool IsEnabled =>
        _settings.CloudReputationEnabled &&
        !string.IsNullOrWhiteSpace(_settings.VirusTotalApiKey);

    public async Task<CloudReputationResult> CheckHashAsync(
        string sha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        if (!IsEnabled)
        {
            return new CloudReputationResult { Status = CloudReputationStatus.NotChecked };
        }

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, $"files/{sha256}");
            request.Headers.Add("x-apikey", _settings.VirusTotalApiKey);

            using HttpResponseMessage response =
                await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new CloudReputationResult { Status = CloudReputationStatus.NotFound };
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "VirusTotal returned {StatusCode} for hash {Sha256}.",
                    response.StatusCode,
                    sha256);

                return new CloudReputationResult { Status = CloudReputationStatus.Unavailable };
            }

            using JsonDocument document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                cancellationToken: cancellationToken);

            return MapResponse(document, sha256);
        }
        catch (Exception exception)
            when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(
                exception,
                "VirusTotal lookup failed for hash {Sha256}.",
                sha256);

            return new CloudReputationResult { Status = CloudReputationStatus.Unavailable };
        }
    }

    /// <summary>
    /// Pure mapping from a VirusTotal v3 "files/{hash}" JSON response to our own
    /// result type. Kept separate from the HTTP call so it can be unit-tested with
    /// canned JSON, without any network access.
    /// </summary>
    internal static CloudReputationResult MapResponse(JsonDocument document, string sha256)
    {
        JsonElement stats = document.RootElement
            .GetProperty("data")
            .GetProperty("attributes")
            .GetProperty("last_analysis_stats");

        int malicious = stats.GetProperty("malicious").GetInt32();
        int suspicious = stats.GetProperty("suspicious").GetInt32();
        int harmless = stats.GetProperty("harmless").GetInt32();
        int undetected = stats.GetProperty("undetected").GetInt32();

        CloudReputationStatus status =
            malicious > 0 ? CloudReputationStatus.Malicious :
            suspicious > 0 ? CloudReputationStatus.Suspicious :
            CloudReputationStatus.Clean;

        return new CloudReputationResult
        {
            Status = status,
            MaliciousCount = malicious,
            SuspiciousCount = suspicious,
            HarmlessCount = harmless,
            UndetectedCount = undetected,
            PermalinkUrl = $"https://www.virustotal.com/gui/file/{sha256}"
        };
    }
}
