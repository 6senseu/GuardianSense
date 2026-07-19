namespace Guardian.Service;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly DownloadWatcher _downloadWatcher;

    public Worker(
        ILogger<Worker> logger,
        DownloadWatcher downloadWatcher)
    {
        _logger = logger;
        _downloadWatcher = downloadWatcher;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Guardian started: {Time}",
            DateTimeOffset.Now);

        _downloadWatcher.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Guardian heartbeat: {Time}",
                    DateTimeOffset.Now);

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normal service shutdown.
        }
        finally
        {
            _downloadWatcher.Stop();
        }
    }

    public override Task StopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Guardian is shutting down: {Time}",
            DateTimeOffset.Now);

        return base.StopAsync(cancellationToken);
    }
}