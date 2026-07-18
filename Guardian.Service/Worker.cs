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
            "Guardian wurde gestartet: {Time}",
            DateTimeOffset.Now);

        _downloadWatcher.Start();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Guardian Heartbeat: {Time}",
                    DateTimeOffset.Now);

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // Normales Beenden des Dienstes.
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
            "Guardian wird beendet: {Time}",
            DateTimeOffset.Now);

        return base.StopAsync(cancellationToken);
    }
}