using Guardian.Analysis.Hashing;
using Guardian.Analysis.Pipeline;

namespace Guardian.Analysis.Providers;

public sealed class HashProvider : IAnalysisProvider
{
    private readonly HashCalculator _hashCalculator;

    public HashProvider(HashCalculator hashCalculator)
    {
        _hashCalculator = hashCalculator;
    }

    public string Name => "SHA-256";

    public int Order => 10;

    public async Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Sha256 =
            await _hashCalculator.CalculateSha256Async(
                context.FilePath,
                cancellationToken);
    }
}