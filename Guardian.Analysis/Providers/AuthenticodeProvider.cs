using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Services;

namespace Guardian.Analysis.Providers;

public sealed class AuthenticodeProvider : IAnalysisProvider
{
    private readonly WinTrustService _winTrustService;

    public string Name => "Authenticode";

    public int Order => 40; // nach Hash, MagicBytes und Risk

    public AuthenticodeProvider(WinTrustService winTrustService)
    {
        _winTrustService = winTrustService;
    }

    public Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        context.Authenticode =
            _winTrustService.VerifyFile(context.FilePath);

        return Task.CompletedTask;
    }
}