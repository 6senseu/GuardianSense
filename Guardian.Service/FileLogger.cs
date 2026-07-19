namespace Guardian.Service;

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _logFilePath;
    private static readonly object LockObject = new();

    public FileLogger(
        string categoryName,
        string logFilePath)
    {
        _categoryName = categoryName;
        _logFilePath = logFilePath;
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string directory =
            Path.GetDirectoryName(_logFilePath)
            ?? throw new InvalidOperationException(
                "Invalid log path.");

        Directory.CreateDirectory(directory);

        string message =
            $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss} " +
            $"[{logLevel}] " +
            $"{_categoryName}: " +
            formatter(state, exception);

        if (exception is not null)
        {
            message += Environment.NewLine + exception;
        }

        lock (LockObject)
        {
            File.AppendAllText(
                _logFilePath,
                message + Environment.NewLine);
        }
    }
}