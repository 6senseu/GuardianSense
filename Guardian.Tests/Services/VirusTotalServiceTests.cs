using System.Text.Json;
using Guardian.Analysis.Services;
using Guardian.Shared.Models;

namespace Guardian.Tests.Services;

public sealed class VirusTotalServiceTests
{
    private const string Sha256 = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcd";

    [Fact]
    public void MapResponse_NoDetections_ReturnsClean()
    {
        using JsonDocument document = CreateResponse(malicious: 0, suspicious: 0, harmless: 70, undetected: 5);

        CloudReputationResult result = VirusTotalService.MapResponse(document, Sha256);

        Assert.Equal(CloudReputationStatus.Clean, result.Status);
        Assert.Equal(70, result.HarmlessCount);
        Assert.Equal(5, result.UndetectedCount);
        Assert.Contains(Sha256, result.PermalinkUrl);
    }

    [Fact]
    public void MapResponse_SomeSuspiciousNoMalicious_ReturnsSuspicious()
    {
        using JsonDocument document = CreateResponse(malicious: 0, suspicious: 2, harmless: 60, undetected: 10);

        CloudReputationResult result = VirusTotalService.MapResponse(document, Sha256);

        Assert.Equal(CloudReputationStatus.Suspicious, result.Status);
        Assert.Equal(2, result.SuspiciousCount);
    }

    [Fact]
    public void MapResponse_AnyMalicious_ReturnsMalicious()
    {
        using JsonDocument document = CreateResponse(malicious: 1, suspicious: 3, harmless: 50, undetected: 10);

        CloudReputationResult result = VirusTotalService.MapResponse(document, Sha256);

        Assert.Equal(CloudReputationStatus.Malicious, result.Status);
        Assert.Equal(1, result.MaliciousCount);
    }

    [Fact]
    public void MapResponse_ManyMalicious_ReturnsMaliciousWithFullCount()
    {
        using JsonDocument document = CreateResponse(malicious: 55, suspicious: 1, harmless: 5, undetected: 3);

        CloudReputationResult result = VirusTotalService.MapResponse(document, Sha256);

        Assert.Equal(CloudReputationStatus.Malicious, result.Status);
        Assert.Equal(55, result.MaliciousCount);
    }

    private static JsonDocument CreateResponse(int malicious, int suspicious, int harmless, int undetected)
    {
        string json = $$"""
        {
          "data": {
            "attributes": {
              "last_analysis_stats": {
                "malicious": {{malicious}},
                "suspicious": {{suspicious}},
                "harmless": {{harmless}},
                "undetected": {{undetected}}
              }
            }
          }
        }
        """;

        return JsonDocument.Parse(json);
    }
}
