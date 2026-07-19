namespace Guardian.Dashboard.ViewModels;

public sealed class MainWindowViewModel
{
    public StatusViewModel Status { get; } = new();

    public ReportsViewModel Reports { get; } = new();

    public QuarantineViewModel Quarantine { get; } = new();

    public SettingsViewModel Settings { get; } = new();
}
