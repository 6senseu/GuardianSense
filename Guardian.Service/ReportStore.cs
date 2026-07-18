using System.Text.Json;
using Guardian.Shared.Models;
using System.Text.Json.Serialization;
namespace Guardian.Service;

public sealed class ReportStore
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            Converters =
            {
            new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase)
            }
        };

    private readonly ILogger<ReportStore> _logger;
    private readonly string _reportDirectory;

    public ReportStore(ILogger<ReportStore> logger)
    {
        _logger = logger;

        _reportDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
            "Guardian",
            "Reports");

        Directory.CreateDirectory(_reportDirectory);
    }

    public async Task<string> SaveAsync(
        FileAnalysisReport report,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        string safeFileName =
            CreateSafeFileName(report.FileName);

        string reportFileName =
            $"{report.DetectedAtUtc:yyyyMMdd-HHmmss}-" +
            $"{safeFileName}-" +
            $"{report.Id:N}.json";

        string finalPath = Path.Combine(
            _reportDirectory,
            reportFileName);

        string temporaryPath = finalPath + ".tmp";

        try
        {
            await using (
                FileStream stream = new(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 16 * 1024,
                    useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    report,
                    JsonOptions,
                    cancellationToken);

                await stream.FlushAsync(cancellationToken);
            }

            File.Move(
                temporaryPath,
                finalPath,
                overwrite: false);

            _logger.LogInformation(
                "JSON-Bericht gespeichert: {ReportPath}",
                finalPath);

            return finalPath;
        }
        catch
        {
            TryDeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    private static string CreateSafeFileName(string fileName)
    {
        string nameWithoutExtension =
            Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(nameWithoutExtension))
        {
            nameWithoutExtension = "unknown";
        }

        foreach (char invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            nameWithoutExtension =
                nameWithoutExtension.Replace(
                    invalidCharacter,
                    '_');
        }

        const int maximumLength = 50;

        if (nameWithoutExtension.Length > maximumLength)
        {
            nameWithoutExtension =
                nameWithoutExtension[..maximumLength];
        }

        return nameWithoutExtension;
    }

    private static void TryDeleteTemporaryFile(
        string temporaryPath)
    {
        try
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
        catch
        {
            // Der ursprüngliche Fehler soll nicht verdeckt werden.
        }
    }
}