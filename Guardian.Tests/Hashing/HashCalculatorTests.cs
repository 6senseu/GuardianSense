using Guardian.Analysis.Hashing;

namespace Guardian.Tests.Hashing;

public sealed class HashCalculatorTests : IDisposable
{
    private readonly HashCalculator _calculator = new();
    private readonly string _tempFile;

    public HashCalculatorTests()
    {
        _tempFile = Path.GetTempFileName();
    }

    [Fact]
    public async Task CalculateSha256Async_KnownContent_ReturnsExpectedHash()
    {
        // SHA-256 of the ASCII bytes "GuardianSense" (no trailing newline), verified independently via sha256sum.
        File.WriteAllText(_tempFile, "GuardianSense");
        const string expectedHash =
            "6e86c6cf4b11ac327c9fc5115f3fb9f6a0c8df5aa7c1ee5d46c26ad0b4b563ad";

        string hash = await _calculator.CalculateSha256Async(_tempFile);

        Assert.Equal(expectedHash, hash);
    }

    [Fact]
    public async Task CalculateSha256Async_SameContent_ReturnsSameHash()
    {
        File.WriteAllText(_tempFile, "identical content");

        string firstHash = await _calculator.CalculateSha256Async(_tempFile);
        string secondHash = await _calculator.CalculateSha256Async(_tempFile);

        Assert.Equal(firstHash, secondHash);
    }

    [Fact]
    public async Task CalculateSha256Async_DifferentContent_ReturnsDifferentHash()
    {
        string otherFile = _tempFile + "_other";

        try
        {
            File.WriteAllText(_tempFile, "content A");
            File.WriteAllText(otherFile, "content B");

            string hashA = await _calculator.CalculateSha256Async(_tempFile);
            string hashB = await _calculator.CalculateSha256Async(otherFile);

            Assert.NotEqual(hashA, hashB);
        }
        finally
        {
            File.Delete(otherFile);
        }
    }

    [Fact]
    public async Task CalculateSha256Async_EmptyPath_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _calculator.CalculateSha256Async(string.Empty));
    }

    public void Dispose()
    {
        File.Delete(_tempFile);
    }
}
