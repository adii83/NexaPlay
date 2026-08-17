using NexaPlay.Contracts.Services;

namespace NexaPlay.Infrastructure.Services;

public sealed class CatalogRefreshState : ICatalogRefreshState
{
    private long _generation;

    public long Generation => Interlocked.Read(ref _generation);

    public long Advance() => Interlocked.Increment(ref _generation);
}
