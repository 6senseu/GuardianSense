using System.Threading;
using System.Windows;
using Guardian.Shared;

namespace Guardian.Dashboard;

public partial class App : Application
{
    private const string MutexName = "GuardianSense.Dashboard.SingleInstance";
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "Guardian Dashboard is already running.",
                "Guardian Dashboard",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Shutdown();
            return;
        }

        // So opening the Dashboard alone (without the Tray) also turns protection on.
        if (!ServiceLauncher.EnsureRunning())
        {
            MessageBox.Show(
                "Guardian.Service.exe was not found next to Guardian.Dashboard.exe. " +
                "The dashboard will still show past reports, but protection is not active " +
                "until the service is running.",
                "Guardian Dashboard",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();

        base.OnExit(e);
    }
}
