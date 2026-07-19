using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Risk;

namespace Guardian.Analysis.Providers;

public sealed class RiskProvider : IAnalysisProvider
{
    private readonly FileRiskAssessor _riskAssessor;

    public RiskProvider(
        FileRiskAssessor riskAssessor)
    {
        _riskAssessor = riskAssessor;
    }

    public string Name => "Local Risk";

    public int Order => 90;

    public Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Calculate the local risk using all available analysis results.
        context.Risk =
            _riskAssessor.Assess(
                context.FilePath,
                context.Signature,
                context.Authenticode,
                context.CloudReputation);

        return Task.CompletedTask;
    }
}