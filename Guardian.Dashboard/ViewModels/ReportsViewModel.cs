using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Guardian.Shared.Models;
using Guardian.Shared.Storage;

namespace Guardian.Dashboard.ViewModels;

public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly ReportReader _reportReader = new();

    public ObservableCollection<FileAnalysisReport> Reports { get; } = new();

    [ObservableProperty]
    private FileAnalysisReport? _selectedReport;

    public ReportsViewModel()
    {
        _ = LoadCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Reports.Clear();

        foreach (FileAnalysisReport report in await _reportReader.GetAllAsync())
        {
            Reports.Add(report);
        }
    }
}
