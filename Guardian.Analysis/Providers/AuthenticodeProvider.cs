using Guardian.Analysis.Pipeline;

namespace Guardian.Analysis.Providers;

public sealed class AuthenticodeProvider : IAnalysisProvider
{
    public string Name => "Authenticode";

    public int Order => 30;

    public Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Authenticode.StatusMessage =
            "Authenticode verification is not implemented yet.";

        return Task.CompletedTask;
    }
}