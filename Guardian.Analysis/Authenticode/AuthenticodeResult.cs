using Guardian.Shared.Models;

namespace Guardian.Analysis.Authenticode;

public sealed class AuthenticodeResult
{
    // ------------------------------------------------------------------------
    // Signature State
    // ------------------------------------------------------------------------

    public bool IsSigned { get; set; }

    public bool IsValid { get; set; }

    public SignatureStatus SignatureStatus { get; set; } =
        SignatureStatus.Unknown;

    // ------------------------------------------------------------------------
    // Certificate Information
    // ------------------------------------------------------------------------

    public string? Publisher { get; set; }

    public string? Issuer { get; set; }

    public DateTimeOffset? ValidFrom { get; set; }

    public DateTimeOffset? ValidUntil { get; set; }

    // ------------------------------------------------------------------------
    // Diagnostics
    // ------------------------------------------------------------------------

    public int NativeResult { get; set; }

    public string StatusMessage { get; set; } = string.Empty;
}