namespace NexaPlay.Core.Helpers;

public sealed class CatalogLoadCoordinator
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _loadedGeneration = -1;

    public long LoadedGeneration => Interlocked.Read(ref _loadedGeneration);

    public async Task RunAsync(
        Func<long> getGeneration,
        bool forceReload,
        Action invalidate,
        Func<long, Task> load)
    {
        await _gate.WaitAsync();
        try
        {
            var generation = getGeneration();
            if (!forceReload && LoadedGeneration == generation)
                return;

            invalidate();
            await load(generation);
            if (getGeneration() == generation)
            {
                Interlocked.Exchange(ref _loadedGeneration, generation);
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
