namespace Guardian.Analysis.Authenticode;

public sealed class AuthenticodeResult
{
    public bool IsSigned { get; set; }

    public bool IsSignatureValid { get; set; }

    public bool IsCertificateCurrentlyValid { get; set; }

    public string Publisher { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Thumbprint { get; set; } = string.Empty;

    public string SerialNumber { get; set; } = string.Empty;

    public DateTimeOffset? ValidFromUtc { get; set; }

    public DateTimeOffset? ValidUntilUtc { get; set; }

    public string StatusMessage { get; set; } = string.Empty;
}