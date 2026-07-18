using System.Collections.Concurrent;
using System.Diagnostics;
using Guardian.Analysis.Pipeline;
using Guardian.Shared;
using Guardian.Shared.Models;
using Microsoft.Extensions.Options;

namespace Guardian.Service;

public sealed class DownloadWatcher : IDisposable
{
    private static readonly HashSet<string> IgnoredExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".crdownload",
            ".part",
            ".partial",
            ".tmp"
        };

    private readonly ILogger<DownloadWatcher> _logger;
    private readonly GuardianSettings _settings;
    private readonly FileSystemWatcher _watcher;
    private readonly ConcurrentDictionary<string, byte> _pendingFiles =
        new();

    private readonly ReportStore _reportStore;
    private readonly AnalysisPipeline _analysisPipeline;

    public DownloadWatcher(
        ILogger<DownloadWatcher> logger,
        IOptions<GuardianSettings> options,
        ReportStore reportStore,
        AnalysisPipeline analysisPipeline)
    {
        _logger = logger;
        _settings = options.Value;
        _reportStore = reportStore;
        _analysisPipeline = analysisPipeline;

        string downloadDirectory =
            ResolveDownloadDirectory(
                _settings.DownloadDirectory);

        Directory.CreateDirectory(downloadDirectory);

        _watcher = new FileSystemWatcher(downloadDirectory)
        {
            IncludeSubdirectories =
                _settings.MonitorSubdirectories,

            NotifyFilter =
                NotifyFilters.FileName |
                NotifyFilters.Size |
                NotifyFilters.LastWrite,

            Filter = "*.*"
        };

        _watcher.Created += OnFileDetected;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;
    }

    public void Start()
    {
        _watcher.EnableRaisingEvents = true;

        _logger.LogInformation(
            """
            Download-Wächter gestartet.
            Ordner: {Directory}
            Unterordner: {IncludeSubdirectories}
            Sprache: {Language}
            """,
            _watcher.Path,
            _watcher.IncludeSubdirectories,
            _settings.Language);
    }

    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;

        _logger.LogInformation(
            "Download-Wächter beendet.");
    }

    private void OnFileDetected(
        object sender,
        FileSystemEventArgs eventArgs)
    {
        QueueFile(eventArgs.FullPath);
    }

    private void OnFileRenamed(
        object sender,
        RenamedEventArgs eventArgs)
    {
        QueueFile(eventArgs.FullPath);
    }

    private void QueueFile(string path)
    {
        if (Directory.Exists(path))
        {
            return;
        }

        string extension = Path.GetExtension(path);

        if (IgnoredExtensions.Contains(extension))
        {
            _logger.LogDebug(
                "Unvollständiger Download ignoriert: {Path}",
                path);

            return;
        }

        if (!_pendingFiles.TryAdd(path, 0))
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await AnalyzeFileAsync(path);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Datei konnte nicht untersucht werden: {Path}",
                    path);
            }
            finally
            {
                _pendingFiles.TryRemove(path, out _);
            }
        });
    }

    private async Task AnalyzeFileAsync(string path)
    {
        bool isReady = await WaitUntilFileIsReadyAsync(
            path,
            TimeSpan.FromSeconds(
                Math.Clamp(
                    _settings.FileReadyTimeoutSeconds,
                    5,
                    300)));

        if (!isReady)
        {
            _logger.LogWarning(
                "Datei blieb gesperrt oder verschwand: {Path}",
                path);

            return;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning(
                "Datei existiert nicht mehr: {Path}",
                path);

            return;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        FileAnalysisReport report =
            await _analysisPipeline.AnalyzeAsync(path);

        stopwatch.Stop();

        string reportPath =
            await _reportStore.SaveAsync(report);

        _logger.LogInformation(
            """
        Neuer Download untersucht:
        Datei: {FileName}
        Pfad: {Path}
        Größe: {Size}
        Endung: {Extension}
        Erkannter Dateityp: {DetectedFileType}
        Dateityp stimmt überein: {TypeMatches}
        SHA-256: {Sha256}
        Lokales Risiko: {RiskLevel}
        Risiko-Score: {RiskScore}/100
        Analysedauer: {DurationMilliseconds:F2} ms
        Bericht: {ReportPath}
        """,
            report.FileName,
            report.OriginalPath,
            FormatFileSize(report.SizeBytes),
            report.Extension,
            report.DetectedFileType,
            report.FileTypeMatchesExtension,
            report.Sha256,
            report.LocalRiskLevel,
            report.LocalRiskScore,
            stopwatch.Elapsed.TotalMilliseconds,
            reportPath);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units =
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        double size = bytes;
        int unitIndex = 0;

        while (size >= 1024 &&
               unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }

    private static async Task<bool> WaitUntilFileIsReadyAsync(
        string path,
        TimeSpan timeout)
    {
        DateTime deadline =
            DateTime.UtcNow + timeout;

        long previousLength = -1;
        int stableChecks = 0;

        while (DateTime.UtcNow < deadline)
        {
            if (!File.Exists(path))
            {
                await Task.Delay(500);
                continue;
            }

            try
            {
                FileInfo file = new(path);

                await using FileStream stream = new(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                if (file.Length == previousLength)
                {
                    stableChecks++;
                }
                else
                {
                    stableChecks = 0;
                    previousLength = file.Length;
                }

                if (stableChecks >= 2)
                {
                    return true;
                }
            }
            catch (IOException)
            {
                stableChecks = 0;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            await Task.Delay(750);
        }

        return false;
    }

    private static string ResolveDownloadDirectory(
        string configuredDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            string expandedPath =
                Environment.ExpandEnvironmentVariables(
                    configuredDirectory);

            return Path.GetFullPath(expandedPath);
        }

        return Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            "Downloads");
    }

    private void OnWatcherError(
        object sender,
        ErrorEventArgs eventArgs)
    {
        _logger.LogError(
            eventArgs.GetException(),
            "Fehler im Download-Wächter.");
    }

    public void Dispose()
    {
        Stop();
        _watcher.Dispose();
    }
}

