using Guardian.Analysis.Pipeline;
using Guardian.Analysis.Signatures;

namespace Guardian.Analysis.Providers;

public sealed class MagicByteProvider : IAnalysisProvider
{
    private readonly FileSignatureDetector _signatureDetector;

    public MagicByteProvider(
        FileSignatureDetector signatureDetector)
    {
        _signatureDetector = signatureDetector;
    }

    public string Name => "Magic Bytes";

    public int Order => 20;

    public async Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Signature =
            await _signatureDetector.DetectAsync(
                context.FilePath,
                cancellationToken);
    }
}