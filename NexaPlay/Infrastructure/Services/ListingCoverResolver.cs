using NexaPlay.Contracts.Services;
using NexaPlay.Core.Helpers;
using System.Collections.Concurrent;

namespace NexaPlay.Infrastructure.Services;

/// <summary>Resolves portrait covers for Games and Home Popular without catalog-wide R2 requests.</summary>
public sealed class ListingCoverResolver : IListingCoverResolver
{
    private const int R2ResultCacheLimit = 512;
    private static readonly TimeSpan R2ResolveTimeout = TimeSpan.FromSeconds(35);

    private readonly INexaPlayOverrideService _overrideService;
    private readonly IGameCoverIndexService _coverIndex;
    private readonly ISteamStoreService _storeService;
    private readonly IAppLogService _log;
    private readonly SemaphoreSlim _r2Gate = new(4, 4);
    private readonly ConcurrentDictionary<int, Lazy<Task<string?>>> _r2Inflight = new();
    private readonly ConcurrentDictionary<int, string> _r2Successes = new();
    private readonly ConcurrentQueue<int> _r2SuccessOrder = new();
    private long _cacheGeneration;

    public ListingCoverResolver(
        INexaPlayOverrideService overrideService,
        IGameCoverIndexService coverIndex,
        ISteamStoreService storeService,
        IAppLogService log)
    {
        _overrideService = overrideService;
        _coverIndex = coverIndex;
        _storeService = storeService;
        _log = log;
    }

    public async Task<string?> ResolveAsync(
        int appId,
        string? runtimeCapsule,
        string? header,
        CancellationToken ct = default)
    {
        var overrideCapsule = await TryGetOverrideAsync(appId, ct);
        if (!string.IsNullOrWhiteSpace(overrideCapsule))
            return overrideCapsule;

        var indexedCapsule = await TryGetIndexedCoverAsync(appId, ct);
        var local = ListingCoverPriority.Select(null, indexedCapsule, runtimeCapsule, null, null);
        if (!string.IsNullOrWhiteSpace(local))
            return local;

        if (!_r2Successes.TryGetValue(appId, out var r2Capsule))
        {
            var generation = Volatile.Read(ref _cacheGeneration);
            var shared = _r2Inflight.GetOrAdd(appId, id => CreateR2Request(id, generation));
            r2Capsule = await shared.Value.WaitAsync(ct);
        }

        return ListingCoverPriority.Select(null, null, null, r2Capsule, header);
    }

    public void ClearCache()
    {
        Interlocked.Increment(ref _cacheGeneration);
        _r2Successes.Clear();
        _r2Inflight.Clear();
        while (_r2SuccessOrder.TryDequeue(out _))
        {
        }
    }

    private Lazy<Task<string?>> CreateR2Request(int appId, long generation)
    {
        Lazy<Task<string?>>? request = null;
        request = new Lazy<Task<string?>>(
            () => GetR2CapsuleAndReleaseAsync(appId, generation, request!),
            LazyThreadSafetyMode.ExecutionAndPublication);
        return request;
    }

    private async Task<string?> GetR2CapsuleAndReleaseAsync(
        int appId,
        long generation,
        Lazy<Task<string?>> request)
    {
        try
        {
            var result = await GetR2CapsuleAsync(appId);
            if (!string.IsNullOrWhiteSpace(result) && generation == Volatile.Read(ref _cacheGeneration))
                CacheSuccessfulResult(appId, result);
            return result;
        }
        finally
        {
            ((ICollection<KeyValuePair<int, Lazy<Task<string?>>>>)_r2Inflight)
                .Remove(new KeyValuePair<int, Lazy<Task<string?>>>(appId, request));
        }
    }

    private void CacheSuccessfulResult(int appId, string result)
    {
        if (!_r2Successes.TryAdd(appId, result))
            return;

        _r2SuccessOrder.Enqueue(appId);
        while (_r2Successes.Count > R2ResultCacheLimit && _r2SuccessOrder.TryDequeue(out var oldest))
            _r2Successes.TryRemove(oldest, out _);
    }

    private async Task<string?> TryGetOverrideAsync(int appId, CancellationToken ct)
    {
        try
        {
            return (await _overrideService.GetCatalogOverrideAsync(appId, ct))?.LibraryCapsule;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Log("CoverResolver", $"Override lookup failed appId={appId}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> TryGetIndexedCoverAsync(int appId, CancellationToken ct)
    {
        try
        {
            return await _coverIndex.GetLibraryCapsuleAsync(appId, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _log.Log("CoverResolver", $"Cover index lookup failed appId={appId}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> GetR2CapsuleAsync(int appId)
    {
        using var timeout = new CancellationTokenSource(R2ResolveTimeout);
        var gateEntered = false;
        try
        {
            await _r2Gate.WaitAsync(timeout.Token);
            gateEntered = true;
            return (await _storeService.GetDetailAsync(appId, timeout.Token))?.LibraryCapsuleUrl;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            _log.Log("CoverResolver", $"R2 capsule fallback timed out appId={appId}");
            return null;
        }
        catch (Exception ex)
        {
            _log.Log("CoverResolver", $"R2 capsule fallback failed appId={appId}: {ex.Message}");
            return null;
        }
        finally
        {
            if (gateEntered)
                _r2Gate.Release();
        }
    }
}
