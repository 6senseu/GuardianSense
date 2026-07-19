using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Guardian.Shared;
using Guardian.Shared.Models;
using Guardian.Shared.Storage;

namespace Guardian.Dashboard.ViewModels;

public sealed partial class StatusViewModel : ObservableObject
{
    private readonly ReportReader _reportReader = new();
    private readonly QuarantineStore _quarantineStore = new();

    [ObservableProperty]
    private bool _isProtectionActive;

    [ObservableProperty]
    private string _statusText = "Checking...";

    [ObservableProperty]
    private DateTimeOffset? _lastScanAtUtc;

    [ObservableProperty]
    private int _scansToday;

    [ObservableProperty]
    private int _activeQuarantineCount;

    public StatusViewModel()
    {
        _ = RefreshCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsProtectionActive =
            File.Exists(GuardianPaths.LogFilePath) &&
            DateTime.Now - File.GetLastWriteTime(GuardianPaths.LogFilePath) < TimeSpan.FromSeconds(30);

        StatusText = IsProtectionActive
            ? "Guardian Service is active."
            : "Guardian Service is not running or unreachable.";

        IReadOnlyList<FileAnalysisReport> reports = await _reportReader.GetAllAsync();

        LastScanAtUtc = reports.Count > 0 ? reports[0].DetectedAtUtc : null;
        ScansToday = reports.Count(report => report.DetectedAtUtc.UtcDateTime.Date == DateTime.UtcNow.Date);

        IReadOnlyList<QuarantineRecord> records = await _quarantineStore.GetAllAsync();
        ActiveQuarantineCount = records.Count(record => record.Status == QuarantineStatus.Quarantined);
    }
}
