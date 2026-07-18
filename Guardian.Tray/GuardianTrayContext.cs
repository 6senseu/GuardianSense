using System.Diagnostics;

namespace Guardian.Tray;

public sealed class GuardianTrayContext
    : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;

    public GuardianTrayContext()
    {
        ContextMenuStrip menu = new();

        ToolStripMenuItem statusItem = new(
            "Guardian ist aktiv")
        {
            Enabled = false
        };

        ToolStripMenuItem openLogsItem =
            new("Protokoll öffnen");

        ToolStripMenuItem exitItem =
            new("Guardian Tray beenden");

        openLogsItem.Click += OpenLogs;
        exitItem.Click += ExitApplication;

        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openLogsItem);
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "Guardian – aktiv",
            ContextMenuStrip = menu,
            Visible = true
        };

        _notifyIcon.DoubleClick += ShowStatus;

        _statusTimer = new System.Windows.Forms.Timer
        {
            Interval = 10_000
        };

        _statusTimer.Tick += CheckStatus;
        _statusTimer.Start();

        ShowStartupNotification();
    }

    private void ShowStartupNotification()
    {
        _notifyIcon.BalloonTipTitle = "Guardian";
        _notifyIcon.BalloonTipText =
            "Der Guardian-Hintergrundwächter ist aktiv.";

        _notifyIcon.ShowBalloonTip(3000);
    }

    private void ShowStatus(
        object? sender,
        EventArgs eventArgs)
    {
        MessageBox.Show(
            "Guardian läuft.\n\n" +
            "Phase 1: Grundsystem aktiv.",
            "Guardian-Status",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void CheckStatus(
        object? sender,
        EventArgs eventArgs)
    {
        string logFilePath = GetLogFilePath();

        bool serviceRecentlyActive =
            File.Exists(logFilePath) &&
            DateTime.Now -
            File.GetLastWriteTime(logFilePath)
            < TimeSpan.FromSeconds(30);

        _notifyIcon.Text = serviceRecentlyActive
            ? "Guardian – aktiv"
            : "Guardian – Dienst nicht erreichbar";

        _notifyIcon.Icon = serviceRecentlyActive
            ? SystemIcons.Shield
            : SystemIcons.Warning;
    }

    private void OpenLogs(
        object? sender,
        EventArgs eventArgs)
    {
        string logFilePath = GetLogFilePath();

        if (!File.Exists(logFilePath))
        {
            MessageBox.Show(
                "Es wurde noch keine Logdatei erstellt.",
                "Guardian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = logFilePath,
            UseShellExecute = true
        });
    }

    private void ExitApplication(
        object? sender,
        EventArgs eventArgs)
    {
        _statusTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        Application.Exit();
    }

    private static string GetLogFilePath()
    {
        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Guardian",
            "Logs",
            "guardian.log");
    }
}