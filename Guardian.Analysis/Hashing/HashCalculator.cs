using System.Security.Cryptography;

namespace Guardian.Analysis.Hashing;

public sealed class HashCalculator
{
    public async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 64 * 1024,
            useAsync: true);

        byte[] hash = await SHA256.HashDataAsync(
            stream,
            cancellationToken);

        return Convert
            .ToHexString(hash)
            .ToLowerInvariant();
    }
}