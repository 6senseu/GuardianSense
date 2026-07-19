using System.Diagnostics;

namespace Guardian.Shared;

/// <summary>
/// Makes sure Guardian.Service is running in the background. Called from both
/// Guardian.Tray and Guardian.Dashboard, so whichever one the user starts first
/// turns protection on - no separate terminal or startup order required.
/// </summary>
public static class ServiceLauncher
{
    private const string ServiceProcessName = "Guardian.Service";

    /// <summary>
    /// Starts Guardian.Service if it is not already running. Returns false when
    /// Guardian.Service.exe could not be found next to the calling app.
    /// </summary>
    public static bool EnsureRunning()
    {
        if (Process.GetProcessesByName(ServiceProcessName).Length > 0)
        {
            return true;
        }

        string serviceExePath = Path.Combine(AppContext.BaseDirectory, "Guardian.Service.exe");

        if (!File.Exists(serviceExePath))
        {
            return false;
        }

        // No console window and not tied to the caller's lifetime: protection
        // should keep running even if the Tray icon or Dashboard is closed later.
        Process.Start(new ProcessStartInfo
        {
            FileName = serviceExePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        return true;
    }
}
