namespace Guardian.Analysis.Native.WinTrust;

/// <summary>
/// Controls whether WinTrust displays a user interface.
/// </summary>
internal enum WinTrustUiChoice : uint
{
    All = 1,
    None = 2,
    NoBad = 3,
    NoGood = 4
}

/// <summary>
/// Specifies which revocation checks WinTrust performs.
/// </summary>
internal enum WinTrustRevocationChecks : uint
{
    None = 0,
    WholeChain = 1
}

/// <summary>
/// Specifies the type of data passed to WinVerifyTrust.
/// </summary>
internal enum WinTrustUnionChoice : uint
{
    File = 1,
    Catalog = 2,
    Blob = 3,
    Signer = 4,
    Certificate = 5
}

/// <summary>
/// Controls the lifetime of the WinTrust state data.
/// </summary>
internal enum WinTrustStateAction : uint
{
    Ignore = 0,
    Verify = 1,
    Close = 2,
    AutoCache = 3,
    AutoCacheFlush = 4
}

/// <summary>
/// Specifies the context in which the signature is verified.
/// </summary>
internal enum WinTrustUiContext : uint
{
    Execute = 0,
    Install = 1
}

/// <summary>
/// Additional behavior flags for WinVerifyTrust.
/// </summary>
[Flags]
internal enum WinTrustProviderFlags : uint
{
    UseIe4TrustFlag = 0x00000001,
    NoIe4ChainFlag = 0x00000002,
    NoPolicyUsageFlag = 0x00000004,
    RevocationCheckNone = 0x00000010,
    RevocationCheckEndCertificate = 0x00000020,
    RevocationCheckChain = 0x00000040,
    RevocationCheckChainExcludeRoot = 0x00000080,
    SaferFlag = 0x00000100,
    HashOnlyFlag = 0x00000200,
    UseDefaultOsVersionCheck = 0x00000400,
    LifetimeSigningFlag = 0x00000800,
    CacheOnlyUrlRetrieval = 0x00001000,
    DisableMd2Md4 = 0x00002000,
    Motw = 0x00004000
}