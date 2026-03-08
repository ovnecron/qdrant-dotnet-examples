using System.Threading.Channels;

namespace Api.Services.Ingestion;

internal sealed class MarkdownIngestionQueue : IMarkdownIngestionQueue
{
    private readonly Channel<QueuedMarkdownIngestionJob> _channel = Channel.CreateUnbounded<QueuedMarkdownIngestionJob>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public Task EnqueueAsync(QueuedMarkdownIngestionJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        return _channel.Writer.WriteAsync(job, cancellationToken).AsTask();
    }

    public ValueTask<QueuedMarkdownIngestionJob> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
