using System.Diagnostics;
using Guardian.Shared;
using Guardian.Shared.Models;
using Guardian.Shared.Storage;
using Guardian.Tray.Services;

namespace Guardian.Tray;

public sealed class GuardianTrayContext
    : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _statusTimer;
    private readonly FileSystemWatcher _quarantineWatcher;
    private readonly HashSet<Guid> _notifiedQuarantineIds = new();
    private readonly bool _serviceStarted;

    public GuardianTrayContext()
    {
        _serviceStarted = ServiceLauncher.EnsureRunning();

        ContextMenuStrip menu = new();

        ToolStripMenuItem statusItem = new(
            "Guardian is active")
        {
            Enabled = false
        };

        ToolStripMenuItem openDashboardItem =
            new("Open Dashboard");

        ToolStripMenuItem openLogsItem =
            new("Open log");

        ToolStripMenuItem startWithWindowsItem = new("Start with Windows")
        {
            CheckOnClick = true,
            Checked = StartupRegistration.IsEnabled
        };

        ToolStripMenuItem exitItem =
            new("Exit Guardian Tray");

        openDashboardItem.Click += OpenDashboard;
        openLogsItem.Click += OpenLogs;
        startWithWindowsItem.Click += ToggleStartWithWindows;
        exitItem.Click += ExitApplication;

        menu.Items.Add(statusItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(openDashboardItem);
        menu.Items.Add(openLogsItem);
        menu.Items.Add(startWithWindowsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Shield,
            Text = "Guardian - active",
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

        Directory.CreateDirectory(GuardianPaths.QuarantineDirectory);

        _quarantineWatcher = new FileSystemWatcher(GuardianPaths.QuarantineDirectory)
        {
            Filter = "*.json",
            NotifyFilter = NotifyFilters.FileName
        };

        // A new quarantine record first appears via an atomic rename (temp file -> final file),
        // so both Created and Renamed need to be handled to catch it.
        _quarantineWatcher.Created += OnQuarantineRecordChanged;
        _quarantineWatcher.Renamed += OnQuarantineRecordChanged;
        _quarantineWatcher.EnableRaisingEvents = true;

        ShowStartupNotification();
    }

    private void ShowStartupNotification()
    {
        _notifyIcon.BalloonTipTitle = "Guardian";

        _notifyIcon.BalloonTipText = _serviceStarted
            ? "The Guardian background watcher is active."
            : "Guardian.Service.exe was not found. Build the Guardian.Service project so protection can start.";

        _notifyIcon.BalloonTipIcon = _serviceStarted
            ? ToolTipIcon.Info
            : ToolTipIcon.Warning;

        _notifyIcon.ShowBalloonTip(3000);
    }

    private void ToggleStartWithWindows(
        object? sender,
        EventArgs eventArgs)
    {
        if (sender is not ToolStripMenuItem item)
        {
            return;
        }

        StartupRegistration.SetEnabled(item.Checked);
    }

    private void ShowStatus(
        object? sender,
        EventArgs eventArgs)
    {
        MessageBox.Show(
            "Guardian is running.",
            "Guardian Status",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void CheckStatus(
        object? sender,
        EventArgs eventArgs)
    {
        bool serviceRecentlyActive =
            File.Exists(GuardianPaths.LogFilePath) &&
            DateTime.Now -
            File.GetLastWriteTime(GuardianPaths.LogFilePath)
            < TimeSpan.FromSeconds(30);

        _notifyIcon.Text = serviceRecentlyActive
            ? "Guardian - active"
            : "Guardian - service unreachable";

        _notifyIcon.Icon = serviceRecentlyActive
            ? SystemIcons.Shield
            : SystemIcons.Warning;
    }

    private void OnQuarantineRecordChanged(
        object sender,
        FileSystemEventArgs eventArgs)
    {
        // Runs on the watcher's background thread - keep it simple and defensive,
        // a failed notification should never crash the tray app.
        try
        {
            QuarantineRecord? record = ReadQuarantineRecord(eventArgs.FullPath);

            if (record is null ||
                record.Status != QuarantineStatus.Quarantined ||
                !_notifiedQuarantineIds.Add(record.Id))
            {
                return;
            }

            _notifyIcon.BalloonTipTitle = "Guardian quarantined a threat";
            _notifyIcon.BalloonTipText =
                $"{record.FileName} was isolated before it could run.\n{record.Reason}";
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;

            _notifyIcon.ShowBalloonTip(5000);
        }
        catch
        {
            // Best-effort notification only; the quarantine action itself already succeeded.
        }
    }

    private static QuarantineRecord? ReadQuarantineRecord(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        return System.Text.Json.JsonSerializer.Deserialize<QuarantineRecord>(
            stream,
            GuardianJsonOptions.Default);
    }

    private void OpenDashboard(
        object? sender,
        EventArgs eventArgs)
    {
        string dashboardExePath = Path.Combine(
            AppContext.BaseDirectory,
            "Guardian.Dashboard.exe");

        if (!File.Exists(dashboardExePath))
        {
            MessageBox.Show(
                "Guardian.Dashboard.exe was not found next to Guardian.Tray.exe. " +
                "Build the Guardian.Dashboard project first.",
                "Guardian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = dashboardExePath,
            UseShellExecute = true
        });
    }

    private void OpenLogs(
        object? sender,
        EventArgs eventArgs)
    {
        if (!File.Exists(GuardianPaths.LogFilePath))
        {
            MessageBox.Show(
                "No log file has been created yet.",
                "Guardian",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = GuardianPaths.LogFilePath,
            UseShellExecute = true
        });
    }

    private void ExitApplication(
        object? sender,
        EventArgs eventArgs)
    {
        _statusTimer.Stop();
        _quarantineWatcher.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        Application.Exit();
    }
}
