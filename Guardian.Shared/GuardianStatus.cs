namespace Guardian.Shared;

public sealed record GuardianStatus(
    bool IsRunning,
    DateTimeOffset LastHeartbeat,
    string Version
);