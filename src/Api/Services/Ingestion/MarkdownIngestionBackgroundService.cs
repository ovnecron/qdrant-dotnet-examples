using Microsoft.Extensions.Hosting;

namespace Api.Services.Ingestion;

internal sealed class MarkdownIngestionBackgroundService : BackgroundService
{
    private readonly IIngestionJobStore _jobStore;
    private readonly ILogger<MarkdownIngestionBackgroundService> _logger;
    private readonly IMarkdownIngestionQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;

    public MarkdownIngestionBackgroundService(
        IMarkdownIngestionQueue queue,
        IIngestionJobStore jobStore,
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<MarkdownIngestionBackgroundService> logger)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _jobStore = jobStore ?? throw new ArgumentNullException(nameof(jobStore));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            QueuedMarkdownIngestionJob job;

            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            _jobStore.MarkRunning(job.JobId, _timeProvider.GetUtcNow());

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IMarkdownIngestionProcessor>();
                var result = await processor.ProcessAsync(job, stoppingToken);
                _jobStore.MarkSucceeded(job.JobId, result, _timeProvider.GetUtcNow());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Markdown ingestion job {JobId} failed.", job.JobId);
                _jobStore.MarkFailed(
                    job.JobId,
                    new IngestionJobError("ingestion_failed", exception.Message),
                    _timeProvider.GetUtcNow());
            }
        }
    }
}
