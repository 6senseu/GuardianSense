using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Services;

namespace Guardian.Analysis.Providers;

public sealed class CloudReputationProvider : IAnalysisProvider
{
    private readonly VirusTotalService _virusTotalService;

    public string Name => "Cloud Reputation";

    public int Order => 50; // after Hash=10 (needs the SHA-256), before RiskProvider=90

    public CloudReputationProvider(VirusTotalService virusTotalService)
    {
        _virusTotalService = virusTotalService;
    }

    public async Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrEmpty(context.Sha256))
        {
            return;
        }

        context.CloudReputation =
            await _virusTotalService.CheckHashAsync(context.Sha256, cancellationToken);
    }
}
