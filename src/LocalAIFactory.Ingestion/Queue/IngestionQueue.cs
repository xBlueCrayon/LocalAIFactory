using System.Threading.Channels;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Queue;

// In-process background queue (singleton). Survives for the lifetime of the app process.
public sealed class IngestionQueue : IIngestionQueue
{
    private readonly Channel<int> _channel =
        Channel.CreateUnbounded<int>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(int ingestionJobId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(ingestionJobId, ct);

    public IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
