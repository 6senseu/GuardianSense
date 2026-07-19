using Guardian.Shared.Models;
using Guardian.Shared.Storage;

namespace Guardian.Tests.Storage;

public sealed class QuarantineStoreTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly QuarantineStore _store;

    public QuarantineStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GuardianTests_Quarantine_" + Guid.NewGuid());
        _store = new QuarantineStore(_tempDirectory);
    }

    [Fact]
    public async Task SaveAsync_ThenGetByIdAsync_ReturnsTheSameRecord()
    {
        QuarantineRecord record = CreateRecord();

        await _store.SaveAsync(record);
        QuarantineRecord? loaded = await _store.GetByIdAsync(record.Id);

        Assert.NotNull(loaded);
        Assert.Equal(record.Id, loaded!.Id);
        Assert.Equal(record.FileName, loaded.FileName);
        Assert.Equal(record.Sha256, loaded.Sha256);
        Assert.Equal(QuarantineStatus.Quarantined, loaded.Status);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        QuarantineRecord? loaded = await _store.GetByIdAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDirectory_ReturnsEmptyList()
    {
        IReadOnlyList<QuarantineRecord> records = await _store.GetAllAsync();

        Assert.Empty(records);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSavedRecords_NewestFirst()
    {
        QuarantineRecord older = CreateRecord(quarantinedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-10));
        QuarantineRecord newer = CreateRecord(quarantinedAtUtc: DateTimeOffset.UtcNow);

        await _store.SaveAsync(older);
        await _store.SaveAsync(newer);

        IReadOnlyList<QuarantineRecord> records = await _store.GetAllAsync();

        Assert.Equal(2, records.Count);
        Assert.Equal(newer.Id, records[0].Id);
        Assert.Equal(older.Id, records[1].Id);
    }

    [Fact]
    public async Task UpdateStatusAsync_Restored_PersistsNewStatus()
    {
        QuarantineRecord record = CreateRecord();
        await _store.SaveAsync(record);

        await _store.UpdateStatusAsync(record.Id, QuarantineStatus.Restored);
        QuarantineRecord? loaded = await _store.GetByIdAsync(record.Id);

        Assert.NotNull(loaded);
        Assert.Equal(QuarantineStatus.Restored, loaded!.Status);
        // Everything else must be preserved.
        Assert.Equal(record.OriginalPath, loaded.OriginalPath);
        Assert.Equal(record.QuarantineFilePath, loaded.QuarantineFilePath);
    }

    [Fact]
    public async Task UpdateStatusAsync_UnknownId_Throws()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _store.UpdateStatusAsync(Guid.NewGuid(), QuarantineStatus.Deleted));
    }

    private static QuarantineRecord CreateRecord(DateTimeOffset? quarantinedAtUtc = null)
    {
        return new QuarantineRecord
        {
            OriginalPath = @"C:\Users\test\Downloads\malware.exe",
            QuarantineFilePath = @"C:\ProgramData\Guardian\Quarantine\Files\malware.exe",
            FileName = "malware.exe",
            Sha256 = "abc123",
            Reason = "Test quarantine reason.",
            RiskScore = 95,
            RiskLevel = RiskLevel.VeryHigh,
            QuarantinedAtUtc = quarantinedAtUtc ?? DateTimeOffset.UtcNow,
            Status = QuarantineStatus.Quarantined
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
