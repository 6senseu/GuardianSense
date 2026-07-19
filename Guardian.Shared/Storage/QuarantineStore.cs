using System.Text.Json;
using Guardian.Shared.Models;

namespace Guardian.Shared.Storage;

/// <summary>
/// Persists quarantine records as one JSON file per record under GuardianPaths.QuarantineDirectory.
/// Used by the Service (creates records when it quarantines a file) and the Dashboard
/// (lists records, restores or deletes quarantined files).
/// </summary>
public sealed class QuarantineStore
{
    private readonly string _quarantineDirectory;

    public QuarantineStore()
        : this(GuardianPaths.QuarantineDirectory)
    {
    }

    /// <summary>
    /// Allows tests to point the store at a temporary directory instead of %ProgramData%.
    /// </summary>
    public QuarantineStore(string quarantineDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quarantineDirectory);

        _quarantineDirectory = quarantineDirectory;
    }

    // Records are named by Id (not by timestamp, unlike report files) because,
    // unlike reports, they need to be looked up and rewritten later (Restore/Delete).
    private string GetFilePath(Guid id) =>
        Path.Combine(_quarantineDirectory, $"{id:N}.json");

    public async Task<string> SaveAsync(
        QuarantineRecord record,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        Directory.CreateDirectory(_quarantineDirectory);

        string finalPath = GetFilePath(record.Id);
        string temporaryPath = finalPath + ".tmp";

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    record,
                    GuardianJsonOptions.Default,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, finalPath, overwrite: true);

            return finalPath;
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    public async Task<IReadOnlyList<QuarantineRecord>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_quarantineDirectory))
        {
            return Array.Empty<QuarantineRecord>();
        }

        List<QuarantineRecord> records = new();

        foreach (string path in Directory.EnumerateFiles(_quarantineDirectory, "*.json"))
        {
            QuarantineRecord? record = await ReadAsync(path, cancellationToken);

            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records
            .OrderByDescending(record => record.QuarantinedAtUtc)
            .ToList();
    }

    public Task<QuarantineRecord?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return ReadAsync(GetFilePath(id), cancellationToken);
    }

    public async Task UpdateStatusAsync(
        Guid id,
        QuarantineStatus status,
        CancellationToken cancellationToken = default)
    {
        QuarantineRecord? existing = await GetByIdAsync(id, cancellationToken)
            ?? throw new FileNotFoundException(
                "No quarantine record found for the given id.",
                GetFilePath(id));

        QuarantineRecord updated = new()
        {
            Id = existing.Id,
            OriginalPath = existing.OriginalPath,
            QuarantineFilePath = existing.QuarantineFilePath,
            FileName = existing.FileName,
            Sha256 = existing.Sha256,
            Reason = existing.Reason,
            RiskScore = existing.RiskScore,
            RiskLevel = existing.RiskLevel,
            QuarantinedAtUtc = existing.QuarantinedAtUtc,
            Status = status
        };

        await SaveAsync(updated, cancellationToken);
    }

    private async Task<QuarantineRecord?> ReadAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            useAsync: true);

        return await JsonSerializer.DeserializeAsync<QuarantineRecord>(
            stream,
            GuardianJsonOptions.Default,
            cancellationToken);
    }

    private static void TryDeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
            // The original error should not be masked.
        }
    }
}
