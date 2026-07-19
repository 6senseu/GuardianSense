using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Guardian.Shared;
using Guardian.Shared.Storage;

namespace Guardian.Dashboard.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _downloadDirectory = string.Empty;

    [ObservableProperty]
    private bool _monitorSubdirectories;

    [ObservableProperty]
    private int _fileReadyTimeoutSeconds = 30;

    [ObservableProperty]
    private bool _cloudReputationEnabled;

    [ObservableProperty]
    private string _virusTotalApiKey = string.Empty;

    [ObservableProperty]
    private string? _statusMessage;

    // Preserves fields the Dashboard UI does not expose (e.g. Language) so Save()
    // does not silently reset them to their defaults.
    private GuardianSettings _loadedSettings = new();

    public SettingsViewModel()
    {
        Load();
    }

    private void Load()
    {
        _loadedSettings = ReadSettingsFile();

        DownloadDirectory = _loadedSettings.DownloadDirectory;
        MonitorSubdirectories = _loadedSettings.MonitorSubdirectories;
        FileReadyTimeoutSeconds = _loadedSettings.FileReadyTimeoutSeconds;
        CloudReputationEnabled = _loadedSettings.CloudReputationEnabled;
        VirusTotalApiKey = _loadedSettings.VirusTotalApiKey;
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            GuardianSettings settings = new()
            {
                Language = _loadedSettings.Language,
                DownloadDirectory = DownloadDirectory,
                MonitorSubdirectories = MonitorSubdirectories,
                FileReadyTimeoutSeconds = FileReadyTimeoutSeconds,
                CloudReputationEnabled = CloudReputationEnabled,
                VirusTotalApiKey = VirusTotalApiKey
            };

            Directory.CreateDirectory(GuardianPaths.RootDirectory);

            var document = new Dictionary<string, GuardianSettings>
            {
                [GuardianSettings.SectionName] = settings
            };

            File.WriteAllText(
                GuardianPaths.SettingsFilePath,
                JsonSerializer.Serialize(document, GuardianJsonOptions.Default));

            StatusMessage = "Settings saved. Restart Guardian Service for the changes to take effect.";
        }
        catch (Exception exception)
        {
            StatusMessage = $"Could not save settings: {exception.Message}";
        }
    }

    private static GuardianSettings ReadSettingsFile()
    {
        if (!File.Exists(GuardianPaths.SettingsFilePath))
        {
            return new GuardianSettings();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(GuardianPaths.SettingsFilePath));

            if (document.RootElement.TryGetProperty(GuardianSettings.SectionName, out JsonElement section))
            {
                return section.Deserialize<GuardianSettings>(GuardianJsonOptions.Default)
                    ?? new GuardianSettings();
            }
        }
        catch (JsonException)
        {
            // Fall back to defaults below if the file is missing or malformed.
        }

        return new GuardianSettings();
    }
}
