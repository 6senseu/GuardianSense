using Guardian.Analysis.Signatures;
using Guardian.Shared.Models;

namespace Guardian.Tests.Signatures;

public sealed class FileSignatureDetectorTests : IDisposable
{
    private readonly FileSignatureDetector _detector = new();
    private readonly string _tempDirectory;

    public FileSignatureDetectorTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GuardianTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
    }

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 }, "exe.exe", "exe")]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "doc.pdf", "pdf")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "image.png", "png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0x00 }, "image.jpg", "jpg")]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "archive.zip", "zip")]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 }, "data.bin", "unknown")]
    public async Task DetectAsync_KnownMagicBytes_ReturnsExpectedType(
        byte[] header,
        string fileName,
        string expectedType)
    {
        string path = CreateFile(fileName, header);

        FileSignatureResult result = await _detector.DetectAsync(path);

        Assert.Equal(expectedType, result.DetectedType);
    }

    [Fact]
    public async Task DetectAsync_ExeContentWithExeExtension_MatchesExtension()
    {
        string path = CreateFile("setup.exe", new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

        FileSignatureResult result = await _detector.DetectAsync(path);

        Assert.True(result.MatchesExtension);
    }

    [Fact]
    public async Task DetectAsync_ExeContentWithPdfExtension_DoesNotMatchExtension()
    {
        string path = CreateFile("invoice.pdf", new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

        FileSignatureResult result = await _detector.DetectAsync(path);

        Assert.False(result.MatchesExtension);
    }

    [Fact]
    public async Task DetectAsync_ZipContentWithDocxExtension_MatchesExtension()
    {
        // .docx/.xlsx/.pptx are ZIP containers under the hood.
        string path = CreateFile("report.docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 });

        FileSignatureResult result = await _detector.DetectAsync(path);

        Assert.True(result.MatchesExtension);
    }

    private string CreateFile(string fileName, byte[] content)
    {
        string path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllBytes(path, content);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
