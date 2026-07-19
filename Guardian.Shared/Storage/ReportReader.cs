using System.Text.Json;
using Guardian.Shared.Models;

namespace Guardian.Shared.Storage;

/// <summary>
/// Read-only access to the JSON reports written by Guardian.Service's ReportStore.
/// Used by the Dashboard to list past scans; report writing stays in Guardian.Service.
/// </summary>
public sealed class ReportReader
{
    private readonly string _reportsDirectory;

    public ReportReader()
        : this(GuardianPaths.ReportsDirectory)
    {
    }

    /// <summary>
    /// Allows tests to point the reader at a temporary directory instead of %ProgramData%.
    /// </summary>
    public ReportReader(string reportsDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportsDirectory);

        _reportsDirectory = reportsDirectory;
    }

    public async Task<IReadOnlyList<FileAnalysisReport>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_reportsDirectory))
        {
            return Array.Empty<FileAnalysisReport>();
        }

        List<FileAnalysisReport> reports = new();

        foreach (string path in Directory.EnumerateFiles(_reportsDirectory, "*.json"))
        {
            FileAnalysisReport? report = await ReadAsync(path, cancellationToken);

            if (report is not null)
            {
                reports.Add(report);
            }
        }

        return reports
            .OrderByDescending(report => report.DetectedAtUtc)
            .ToList();
    }

    public async Task<FileAnalysisReport?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FileAnalysisReport> reports = await GetAllAsync(cancellationToken);

        return reports.FirstOrDefault(report => report.Id == id);
    }

    private static async Task<FileAnalysisReport?> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = new(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 16 * 1024,
                useAsync: true);

            return await JsonSerializer.DeserializeAsync<FileAnalysisReport>(
                stream,
                GuardianJsonOptions.Default,
                cancellationToken);
        }
        catch (JsonException)
        {
            // Skip reports that cannot be parsed (e.g. a write was interrupted).
            return null;
        }
    }
}
