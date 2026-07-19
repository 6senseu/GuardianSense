using System.Diagnostics;
using System.IO;
using System.Security;
using Guardian.Shared.Models;

namespace Guardian.Dashboard.Services;

/// <summary>
/// Lets the user inspect a quarantined file inside Windows' built-in disposable VM
/// instead of on the real system. Guardian never auto-executes the file, even here -
/// the sandbox only opens with the file mapped in; the user decides whether to run it.
/// </summary>
public sealed class SandboxLauncher
{
    private static readonly string WindowsSandboxExePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "WindowsSandbox.exe");

    public bool IsAvailable => File.Exists(WindowsSandboxExePath);

    public void Launch(QuarantineRecord record)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                "Windows Sandbox is not available. It requires Windows 10/11 Pro, " +
                "Enterprise or Education with the 'Windows Sandbox' optional feature enabled.");
        }

        if (!File.Exists(record.QuarantineFilePath))
        {
            throw new FileNotFoundException(
                "The quarantined file no longer exists.",
                record.QuarantineFilePath);
        }

        // Stage a copy in its own folder instead of mapping the whole quarantine
        // directory, so the sandbox only ever sees the one file the user picked.
        string stagingDirectory = Path.Combine(
            Path.GetTempPath(),
            $"guardian-sandbox-{Guid.NewGuid():N}");

        Directory.CreateDirectory(stagingDirectory);
        File.Copy(
            record.QuarantineFilePath,
            Path.Combine(stagingDirectory, record.FileName),
            overwrite: true);

        string configPath = stagingDirectory + ".wsb";
        File.WriteAllText(configPath, BuildConfig(stagingDirectory));

        Process.Start(new ProcessStartInfo
        {
            FileName = WindowsSandboxExePath,
            Arguments = $"\"{configPath}\"",
            UseShellExecute = true
        });
    }

    private static string BuildConfig(string hostFolder)
    {
        // No LogonCommand on purpose: the sandbox opens with the file mapped in
        // read-only, but Guardian never runs it automatically, even in isolation.
        return $"""
            <Configuration>
              <MappedFolders>
                <MappedFolder>
                  <HostFolder>{SecurityElement.Escape(hostFolder)}</HostFolder>
                  <ReadOnly>true</ReadOnly>
                </MappedFolder>
              </MappedFolders>
            </Configuration>
            """;
    }
}
