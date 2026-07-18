using System.Diagnostics;
using Guardian.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Guardian.Analysis.Pipeline;

public sealed class AnalysisPipeline
{
    private readonly IReadOnlyList<IAnalysisProvider> _providers;
    private readonly ILogger<AnalysisPipeline> _logger;

    public AnalysisPipeline(
        IEnumerable<IAnalysisProvider> providers,
        ILogger<AnalysisPipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);

        _providers = providers
            .OrderBy(provider => provider.Order)
            .ToList();

        _logger = logger;
    }

    public async Task<FileAnalysisReport> AnalyzeAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        string fullPath = Path.GetFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Die zu analysierende Datei wurde nicht gefunden.",
                fullPath);
        }

        AnalysisContext context = new(fullPath);

        Stopwatch totalStopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Analysis started for {FilePath} with {ProviderCount} providers.",
            fullPath,
            _providers.Count);

        foreach (IAnalysisProvider provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Stopwatch providerStopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug(
                    "Starting provider {ProviderName} with order {ProviderOrder}.",
                    provider.Name,
                    provider.Order);

                await provider.AnalyzeAsync(
                    context,
                    cancellationToken);

                providerStopwatch.Stop();

                _logger.LogInformation(
                    "Provider {ProviderName} completed in {ElapsedMilliseconds} ms.",
                    provider.Name,
                    providerStopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                providerStopwatch.Stop();

                _logger.LogWarning(
                    "Provider {ProviderName} was cancelled after {ElapsedMilliseconds} ms.",
                    provider.Name,
                    providerStopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception exception)
            {
                providerStopwatch.Stop();

                string errorMessage =
                    $"{provider.Name}: {exception.Message}";

                context.Errors.Add(errorMessage);

                _logger.LogError(
                    exception,
                    "Provider {ProviderName} failed after {ElapsedMilliseconds} ms.",
                    provider.Name,
                    providerStopwatch.ElapsedMilliseconds);
            }
        }

        context.CompletedAtUtc = DateTimeOffset.UtcNow;

        totalStopwatch.Stop();

        FileInfo file = new(fullPath);
        file.Refresh();

        string status =
            context.HasErrors
                ? "completed-with-errors"
                : "completed";

        _logger.LogInformation(
            "Analysis finished for {FilePath} with status {Status} in {ElapsedMilliseconds} ms.",
            fullPath,
            status,
            totalStopwatch.ElapsedMilliseconds);

        return new FileAnalysisReport
        {
            DetectedAtUtc = context.StartedAtUtc,
            Status = status,

            FileName = file.Name,
            OriginalPath = file.FullName,
            Extension = file.Extension,

            Sha256 = context.Sha256,

            DetectedFileType =
                context.Signature.DetectedType,

            FileTypeMatchesExtension =
                context.Signature.MatchesExtension,

            FileSignature =
                context.Signature.Signature,

            SizeBytes = file.Length,

            CreatedAtUtc =
                new DateTimeOffset(file.CreationTimeUtc),

            LastModifiedAtUtc =
                new DateTimeOffset(file.LastWriteTimeUtc),

            LocalRiskScore =
                context.Risk.Score,

            LocalRiskLevel =
                context.Risk.Level,

            LocalRiskCategory =
                context.Risk.Category,

            LocalRiskReasons =
                context.Risk.Reasons,

            Source = "download-watcher",
            GuardianVersion = "0.3.0"
        };
    }
}