using Guardian.Analysis.Authenticode;
using Guardian.Analysis.Native.WinTrust;
using Guardian.Shared.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Guardian.Analysis.Services;

/// <summary>
/// Verifies a file's Authenticode signature and reads additional
/// certificate information such as publisher and validity period.
/// </summary>
public sealed class WinTrustService
{
    /// <summary>
    /// The file has no embedded signature.
    /// </summary>
    private const int TrustENoSignature =
        unchecked((int)0x800B0100);

    /// <summary>
    /// The file or its signature was tampered with.
    /// </summary>
    private const int TrustEBadDigest =
        unchecked((int)0x80096010);

    /// <summary>
    /// The signing certificate has expired.
    /// </summary>
    private const int CertEExpired =
        unchecked((int)0x800B0101);

    /// <summary>
    /// The signing certificate has been revoked.
    /// </summary>
    private const int CertERevoked =
        unchecked((int)0x800B010C);

    private readonly ILogger<WinTrustService> _logger;

    public WinTrustService(ILogger<WinTrustService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs the Authenticode check for a file.
    /// </summary>
    /// <param name="filePath">Path to the file to verify.</param>
    /// <returns>The signature verification result.</returns>
    public AuthenticodeResult VerifyFile(string filePath)
    {
        // Hands the file over to WinVerifyTrust.
        var fileInfo = new WinTrustFileInfo(filePath);

        // Builds the required WinTrust structure.
        using var data = new WinTrustData(fileInfo);

        // Performs the actual Windows signature check.
        int result = WinTrustNative.WinVerifyTrust(
            IntPtr.Zero,
            WinTrustGuids.WintrustActionGenericVerifyV2,
            data);

        // Builds our internal result object.
        var authenticode = CreateResult(result);

        // Only try to read additional certificate information
        // when the file is actually signed.
        if (authenticode.IsSigned)
        {
            try
            {
#pragma warning disable SYSLIB0057
                using X509Certificate certificate =
                    X509Certificate.CreateFromSignedFile(filePath);
#pragma warning restore SYSLIB0057

                using var certificate2 =
                    new X509Certificate2(certificate);

                // Publisher display name.
                authenticode.Publisher =
                    certificate2.GetNameInfo(
                        X509NameType.SimpleName,
                        false);

                // Certificate authority.
                authenticode.Issuer =
                    certificate2.GetNameInfo(
                        X509NameType.SimpleName,
                        true);

                // Start of certificate validity.
                authenticode.ValidFrom =
                    certificate2.NotBefore;

                // End of certificate validity.
                authenticode.ValidUntil =
                    certificate2.NotAfter;
            }
            catch (CryptographicException exception)
            {
                // Certificate details could not be read.
                // The signature check result itself remains valid.
                _logger.LogWarning(
                    exception,
                    "Could not read certificate details for {FilePath}.",
                    filePath);
            }
        }

        return authenticode;
    }

    /// <summary>
    /// Maps the native WinVerifyTrust return value
    /// to an AuthenticodeResult.
    /// </summary>
    private static AuthenticodeResult CreateResult(int result)
    {
        return result switch
        {
            // Signature is fully valid.
            0 => new AuthenticodeResult
            {
                IsSigned = true,
                IsValid = true,
                SignatureStatus = SignatureStatus.Valid,
                NativeResult = result,
                StatusMessage = "The signature is valid."
            },

            // File has no embedded signature.
            TrustENoSignature => new AuthenticodeResult
            {
                IsSigned = false,
                IsValid = false,
                SignatureStatus = SignatureStatus.NotSigned,
                NativeResult = result,
                StatusMessage =
                    "No embedded Authenticode signature found."
            },

            // Signature no longer matches the file.
            TrustEBadDigest => new AuthenticodeResult
            {
                IsSigned = true,
                IsValid = false,
                SignatureStatus = SignatureStatus.Invalid,
                NativeResult = result,
                StatusMessage =
                    "The file or its signature was tampered with."
            },

            // Certificate has expired.
            CertEExpired => new AuthenticodeResult
            {
                IsSigned = true,
                IsValid = false,
                SignatureStatus = SignatureStatus.Expired,
                NativeResult = result,
                StatusMessage =
                    "The signing certificate has expired."
            },

            // Certificate has been revoked.
            CertERevoked => new AuthenticodeResult
            {
                IsSigned = true,
                IsValid = false,
                SignatureStatus = SignatureStatus.Revoked,
                NativeResult = result,
                StatusMessage =
                    "The signing certificate has been revoked."
            },

            // Unknown WinVerifyTrust error.
            _ => new AuthenticodeResult
            {
                IsSigned = true,
                IsValid = false,
                SignatureStatus = SignatureStatus.Unknown,
                NativeResult = result,
                StatusMessage =
                    $"WinVerifyTrust returned 0x{result:X8}."
            }
        };
    }
}