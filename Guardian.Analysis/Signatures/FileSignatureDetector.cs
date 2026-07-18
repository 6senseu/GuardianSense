using Guardian.Shared.Models;

namespace Guardian.Analysis.Signatures;

public sealed class FileSignatureDetector
{
    public async Task<FileSignatureResult> DetectAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        byte[] buffer = new byte[16];

        await using FileStream stream = new(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true);

        int bytesRead = await stream.ReadAsync(
            buffer.AsMemory(0, buffer.Length),
            cancellationToken);

        string hex = Convert.ToHexString(
            buffer.AsSpan(0, bytesRead));

        if (StartsWith(buffer, bytesRead, 0x4D, 0x5A))
        {
            return Create(path, "exe", hex);
        }

        if (StartsWith(
                buffer,
                bytesRead,
                0x25, 0x50, 0x44, 0x46))
        {
            return Create(path, "pdf", hex);
        }

        if (StartsWith(
                buffer,
                bytesRead,
                0x89, 0x50, 0x4E, 0x47))
        {
            return Create(path, "png", hex);
        }

        if (StartsWith(buffer, bytesRead, 0xFF, 0xD8, 0xFF))
        {
            return Create(path, "jpg", hex);
        }

        if (StartsWith(
                buffer,
                bytesRead,
                0x50, 0x4B, 0x03, 0x04))
        {
            return Create(path, "zip", hex);
        }

        return Create(path, "unknown", hex);
    }

    private static bool StartsWith(
        byte[] buffer,
        int bytesRead,
        params byte[] signature)
    {
        if (bytesRead < signature.Length)
        {
            return false;
        }

        return buffer
            .AsSpan(0, signature.Length)
            .SequenceEqual(signature);
    }

    private static FileSignatureResult Create(
        string path,
        string detectedType,
        string signature)
    {
        string extension = Path
            .GetExtension(path)
            .TrimStart('.')
            .ToLowerInvariant();

        bool matchesExtension =
            detectedType == "unknown" ||
            ExtensionMatchesDetectedType(
                extension,
                detectedType);

        return new FileSignatureResult
        {
            DetectedType = detectedType,
            Signature = signature,
            MatchesExtension = matchesExtension
        };
    }

    private static bool ExtensionMatchesDetectedType(
        string extension,
        string detectedType)
    {
        return detectedType switch
        {
            "exe" => extension is "exe" or "dll" or "scr" or "cpl",
            "jpg" => extension is "jpg" or "jpeg",
            "zip" => extension is "zip" or "docx" or "xlsx" or "pptx" or
                "vsdx" or "odt" or "ods" or "odp" or "jar" or "nupkg",
            _ => extension == detectedType
        };
    }
}