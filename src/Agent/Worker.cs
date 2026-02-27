namespace Agent;

public class Worker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
