namespace Guardian.Shared;

/// <summary>
/// Resolves the shared %ProgramData%\Guardian folder structure used by
/// the Service, Tray and Dashboard to exchange logs, reports and
/// quarantine data without a dedicated IPC channel.
/// </summary>
public static class GuardianPaths
{
    public static string RootDirectory =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Guardian");

    public static string LogsDirectory =>
        Path.Combine(RootDirectory, "Logs");

    public static string LogFilePath =>
        Path.Combine(LogsDirectory, "guardian.log");

    public static string ReportsDirectory =>
        Path.Combine(RootDirectory, "Reports");

    public static string QuarantineDirectory =>
        Path.Combine(RootDirectory, "Quarantine");

    public static string QuarantineFilesDirectory =>
        Path.Combine(QuarantineDirectory, "Files");

    /// <summary>
    /// User-editable settings, shared between the Service and the Dashboard.
    /// The Service loads this on top of its own appsettings.json defaults; the
    /// Dashboard's Settings view reads and writes it directly. The Service must
    /// be restarted to pick up changes (no live IPC in this MVP).
    /// </summary>
    public static string SettingsFilePath =>
        Path.Combine(RootDirectory, "settings.json");
}
