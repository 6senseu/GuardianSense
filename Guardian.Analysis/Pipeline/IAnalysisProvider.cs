namespace Guardian.Analysis.Pipeline;

public interface IAnalysisProvider
{
    string Name { get; }

    int Order { get; }

    Task AnalyzeAsync(
        AnalysisContext context,
        CancellationToken cancellationToken = default);
}