namespace Guardian.Shared.Models;

public enum SignatureStatus
{
    Unknown = 0,

    Valid,

    NotSigned,

    Invalid,

    Expired,

    Revoked
}