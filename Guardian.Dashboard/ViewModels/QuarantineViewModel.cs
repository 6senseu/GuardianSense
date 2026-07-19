using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Guardian.Dashboard.Services;
using Guardian.Shared.Models;
using Guardian.Shared.Storage;

namespace Guardian.Dashboard.ViewModels;

public sealed partial class QuarantineViewModel : ObservableObject
{
    private readonly QuarantineStore _quarantineStore = new();
    private readonly SandboxLauncher _sandboxLauncher = new();

    public ObservableCollection<QuarantineRecord> Records { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInSandboxCommand))]
    private QuarantineRecord? _selectedRecord;

    [ObservableProperty]
    private string? _statusMessage;

    public QuarantineViewModel()
    {
        _ = LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Records.Clear();

        foreach (QuarantineRecord record in await _quarantineStore.GetAllAsync())
        {
            Records.Add(record);
        }
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelectedRecord))]
    private async Task RestoreAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        try
        {
            string? originalDirectory = Path.GetDirectoryName(SelectedRecord.OriginalPath);

            if (!string.IsNullOrEmpty(originalDirectory))
            {
                Directory.CreateDirectory(originalDirectory);
            }

            File.Move(SelectedRecord.QuarantineFilePath, SelectedRecord.OriginalPath, overwrite: false);
            await _quarantineStore.UpdateStatusAsync(SelectedRecord.Id, QuarantineStatus.Restored);

            StatusMessage = $"Restored {SelectedRecord.FileName} to {SelectedRecord.OriginalPath}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not restore {SelectedRecord.FileName}: {exception.Message}";
        }

        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelectedRecord))]
    private async Task DeleteAsync()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        try
        {
            if (File.Exists(SelectedRecord.QuarantineFilePath))
            {
                File.Delete(SelectedRecord.QuarantineFilePath);
            }

            await _quarantineStore.UpdateStatusAsync(SelectedRecord.Id, QuarantineStatus.Deleted);

            StatusMessage = $"Permanently deleted {SelectedRecord.FileName}.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not delete {SelectedRecord.FileName}: {exception.Message}";
        }

        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanActOnSelectedRecord))]
    private void OpenInSandbox()
    {
        if (SelectedRecord is null)
        {
            return;
        }

        try
        {
            _sandboxLauncher.Launch(SelectedRecord);
            StatusMessage = $"Opened {SelectedRecord.FileName} in Windows Sandbox (read-only, nothing runs automatically).";
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
    }

    private bool CanActOnSelectedRecord() =>
        SelectedRecord is { Status: QuarantineStatus.Quarantined };
}
