using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
    private readonly IAddGameService _addGame;
    private readonly IMetadataService _metadata;
    private readonly ISteamStoreService _storeService;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly IAppLogService _log;

    [ObservableProperty] public partial int TotalFixes { get; set; }
    [ObservableProperty] public partial int LibraryCount { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial IReadOnlyList<FixEntry> RecentFixes { get; set; }
    [ObservableProperty] public partial ObservableCollection<GameEntry> PopularGames { get; set; }
    [ObservableProperty] public partial FixEntry? HeroGame { get; set; }

    private IReadOnlyList<int> _allPopularAppIds = Array.Empty<int>();
    private readonly List<GameEntry> _loadedPopularCache = new();
    private int _currentPopularPage = 0;
    private int _popularColumns = 5;
    private int PopularGamesPageSize => _popularColumns * 8;
    private const int PopularFetchConcurrency = 6;
    private readonly SemaphoreSlim _apiEnrichLock = new(1, 1);

    public HomeViewModel(
        IBypassGamesDataService fixData,
        IAddGameService addGame,
        IMetadataService metadata,
        ISteamStoreService storeService,
        INexaPlayOverrideService nexaPlayOverride,
        IAppLogService log)
    {
        _fixData = fixData;
        _addGame = addGame;
        _metadata = metadata;
        _storeService = storeService;
        _nexaPlayOverride = nexaPlayOverride;
        _log = log;

        // Default values for partial properties
        IsLoading    = true;
        RecentFixes  = Array.Empty<FixEntry>();
        PopularGames = new ObservableCollection<GameEntry>();
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var fixes = await _fixData.GetAllFixesAsync();
            TotalFixes   = fixes.Count;
            LibraryCount = _addGame.ListLibraryGames().Count;

            var newFixAppIds = await _metadata.GetNewFixAppIdsAsync();
            if (newFixAppIds.Count > 0)
            {
                var recent = new List<FixEntry>();
                foreach (var appId in newFixAppIds.Take(12))
                {
                    var meta = await _metadata.GetMetadataAsync(appId);
                    recent.Add(new FixEntry
                    {
                        AppId = appId,
                        Title = meta?.Name ?? $"App {appId}",
                        Publisher = meta?.PublisherDisplay ?? string.Empty,
                        Category = GameCategory.Other,
                        PosterUrl = meta?.LibraryHero2xUrl ?? meta?.LibraryCapsule2xUrl ?? meta?.RawHeaderImageUrl,
                        IsPremium = meta?.IsPremium ?? false
                    });
                }
                recent = await EnrichRecentFixesHeroCoverAsync(recent);
                HeroGame = recent.FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.PosterUrl)) ?? recent.FirstOrDefault();
                RecentFixes = recent;
            }
            else
            {
                RecentFixes = Array.Empty<FixEntry>();
                HeroGame = null;
                _log.Log("Home", "new_fix_games appid list is empty.");
            }
        }
        finally 
        { 
            IsLoading = false; 
        }

        // Fire and forget loading of popular games (Lazy Load)
        _ = LoadPopularGamesInBackgroundAsync();
    }

    private async Task LoadPopularGamesInBackgroundAsync()
    {
        // Prevent resetting the list and scroll state if already loaded (e.g., returning from GameDetail)
        if (_allPopularAppIds.Count > 0 && PopularGames.Count > 0)
        {
            return;
        }

        try
        {
            _allPopularAppIds = await _metadata.GetPopularAppIdsAsync();
            _currentPopularPage = 0;
            _log.Log("Home", $"Popular app ids loaded: {_allPopularAppIds.Count}");

            var popularList = await LoadNextPopularPageAsync();

            _loadedPopularCache.Clear();
            _loadedPopularCache.AddRange(popularList);
            PopularGames = new ObservableCollection<GameEntry>(popularList.Take(PopularGamesPageSize));
            _log.Log("Home", $"Popular games rendered: {PopularGames.Count}");

            // Fire background enrichment from real Steam API (reads disk cache first, no blocking)
            _ = EnrichPopularCoversFromApiAsync(PopularGames.ToList());

            if (popularList.Count > 0 && popularList.All(g => !g.HasPopularCover))
            {
                _log.Log("Home", "No library_capsule_2x/header cover in current cache. Triggering background metadata refresh for Home.");
                _ = RefreshPopularCoversInBackgroundAsync();
            }
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"Popular games load failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task LoadMorePopularGamesAsync()
    {
        if (_allPopularAppIds.Count == 0 || _currentPopularPage >= _allPopularAppIds.Count) return;

        var targetRows = 4;
        var targetCount = _popularColumns * targetRows;
        var newGames = await LoadNextPopularPageAsync(targetCount);

        foreach(var g in newGames)
        {
            _loadedPopularCache.Add(g);
            PopularGames.Add(g);
        }

        await EnsurePopularFilledRowsAsync();
        _ = EnrichPopularCoversFromApiAsync(newGames);
    }

    public async Task UpdatePopularLayoutAsync(int columns)
    {
        columns = Math.Clamp(columns, 3, 6);
        _popularColumns = columns;
        if (_loadedPopularCache.Count == 0)
            return;

        // Maintain user's scroll depth by keeping the maximum of currently displayed items or 8 full rows
        var baseItemCount = Math.Max(PopularGames.Count, _popularColumns * 8);
        var neededRows = (int)Math.Ceiling((double)baseItemCount / _popularColumns);
        var needed = neededRows * _popularColumns;
        while (_loadedPopularCache.Count < needed && _currentPopularPage < _allPopularAppIds.Count)
        {
            var more = await LoadNextPopularPageAsync(_popularColumns * 2);
            if (more.Count == 0)
                break;
            _loadedPopularCache.AddRange(more);
            _ = EnrichPopularCoversFromApiAsync(more);
        }

        var targetCount = Math.Min(needed, _loadedPopularCache.Count);
        if (PopularGames.Count > targetCount)
        {
            for (var i = PopularGames.Count - 1; i >= targetCount; i--)
            {
                PopularGames.RemoveAt(i);
            }
        }
        else if (PopularGames.Count < targetCount)
        {
            for (var i = PopularGames.Count; i < targetCount; i++)
            {
                PopularGames.Add(_loadedPopularCache[i]);
            }
        }

        await EnsurePopularFilledRowsAsync();
    }

    private async Task EnsurePopularFilledRowsAsync()
    {
        if (_popularColumns <= 0)
            return;

        var remainder = PopularGames.Count % _popularColumns;
        if (remainder == 0)
            return;

        var required = _popularColumns - remainder;
        while (required > 0 && _currentPopularPage < _allPopularAppIds.Count)
        {
            var batch = await LoadNextPopularPageAsync(required);
            if (batch.Count == 0)
                break;

            foreach (var g in batch)
            {
                _loadedPopularCache.Add(g);
                PopularGames.Add(g);
            }

            required -= batch.Count;
            _ = EnrichPopularCoversFromApiAsync(batch);
        }
    }

    private async Task<List<GameEntry>> LoadNextPopularPageAsync(int targetCount = -1)
    {
        var requestSize = targetCount > 0 ? targetCount : PopularGamesPageSize;
        var finalResults = new List<GameEntry>(requestSize);

        // Keep fetching until we fulfill the requested quota of VALID (non-null) items, or we run out of catalog IDs
        while (finalResults.Count < requestSize && _currentPopularPage < _allPopularAppIds.Count)
        {
            var needed = requestSize - finalResults.Count;
            var pageAppIds = new List<int>(needed);
            
            while (pageAppIds.Count < needed && _currentPopularPage < _allPopularAppIds.Count)
            {
                pageAppIds.Add(_allPopularAppIds[_currentPopularPage++]);
            }

            if (pageAppIds.Count == 0)
                break;

            var results = new GameEntry?[pageAppIds.Count];
            using var gate = new SemaphoreSlim(PopularFetchConcurrency, PopularFetchConcurrency);

            var tasks = pageAppIds.Select(async (appId, idx) =>
            {
                await gate.WaitAsync();
                try
                {
                    results[idx] = await _metadata.GetMetadataAsync(appId);
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(tasks);

            foreach (var g in results)
            {
                if (g is not null)
                {
                    finalResults.Add(g);
                }
            }
        }

        return finalResults;
    }

    private async Task RefreshPopularCoversInBackgroundAsync()
    {
        try
        {
            await _metadata.RefreshAsync(forceDownload: false);
            var refreshed = new List<GameEntry>(PopularGamesPageSize);
            foreach (var appId in _allPopularAppIds.Take(PopularGamesPageSize))
            {
                var metadata = await _metadata.GetMetadataAsync(appId);
                if (metadata is not null)
                    refreshed.Add(metadata);
            }

            if (refreshed.Count == 0)
                return;

            PopularGames.Clear();
            foreach (var game in refreshed)
            {
                PopularGames.Add(game);
            }
            _log.Log("Home", $"Popular games refreshed after metadata sync: {PopularGames.Count}");
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"Background popular cover refresh failed: {ex.Message}");
        }
    }

    private async Task EnrichPopularCoversFromApiAsync(IReadOnlyList<GameEntry>? targetGames = null)
    {
        var scope = (targetGames is { Count: > 0 } ? targetGames : PopularGames.ToList());
        if (scope.Count == 0)
            return;

        if (!await _apiEnrichLock.WaitAsync(0))
            return;

        try
        {
            var candidates = scope
                .Select((game, index) => new { game, index })
                .Where(x =>
                    string.IsNullOrWhiteSpace(x.game.LibraryCapsule2xUrl) ||
                    (!string.IsNullOrWhiteSpace(x.game.RawHeaderImageUrl) &&
                     string.Equals(x.game.PopularCoverImageUrl, x.game.RawHeaderImageUrl, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (candidates.Count == 0)
                return;

            using var gate = new SemaphoreSlim(4, 4);
            var updateTasks = candidates.Select(async c =>
            {
                await gate.WaitAsync();
                try
                {
                    var detail = await _storeService.GetDetailAsync(c.game.AppId);
                    var apiCover = detail?.LibraryCapsule2xUrl;
                    if (string.IsNullOrWhiteSpace(apiCover))
                        return;

                    var current = PopularGames.FirstOrDefault(g => g.AppId == c.game.AppId);
                    if (current is null)
                        return;

                    if (string.Equals(current.LibraryCapsule2xUrl, apiCover, StringComparison.OrdinalIgnoreCase))
                        return;

                    var idx = PopularGames.IndexOf(current);
                    if (idx < 0)
                        return;

                    PopularGames[idx] = new GameEntry
                    {
                        AppId = current.AppId,
                        Name = current.Name,
                        Developer = current.Developer,
                        Publisher = current.Publisher,
                        Developers = current.Developers,
                        Publishers = current.Publishers,
                        Genre = current.Genre,
                        ShortDescription = current.ShortDescription,
                        ReleaseDate = current.ReleaseDate,
                        PriceNormalized = current.PriceNormalized,
                        PriceDisplay = current.PriceDisplay,
                        Protection = current.Protection,
                        HeaderImageUrl = current.HeaderImageUrl,
                        IconImageUrl = current.IconImageUrl,
                        LibraryCapsule2xUrl = apiCover,
                        LibraryHero2xUrl = current.LibraryHero2xUrl,
                        BackgroundRawImageUrl = current.BackgroundRawImageUrl,
                        RawMetadataJson = current.RawMetadataJson,
                        RawFieldPathCount = current.RawFieldPathCount
                    };
                }
                catch (Exception ex)
                {
                    _log.Log("Home", $"API cover enrichment failed appId={c.game.AppId}: {ex.Message}");
                }
                finally
                {
                    gate.Release();
                }
            });

            await Task.WhenAll(updateTasks);
            _log.Log("Home", $"API cover enrichment done. candidates={candidates.Count}, current={PopularGames.Count}");
        }
        catch (Exception ex)
        {
            _log.Log("Home", $"API cover enrichment failed: {ex.Message}");
        }
        finally
        {
            _apiEnrichLock.Release();
        }
    }

    private async Task<List<FixEntry>> EnrichRecentFixesHeroCoverAsync(List<FixEntry> recent)
    {
        if (recent.Count == 0)
            return recent;

        var results = new FixEntry[recent.Count];
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = recent.Select(async (fix, idx) =>
        {
            await gate.WaitAsync();
            try
            {
                var heroCover = fix.PosterUrl;

                var catalogOv = await _nexaPlayOverride.GetCatalogOverrideAsync(fix.AppId);
                var hasHeroOverride = catalogOv?.LibraryHero2x is not null;
                var hasCapsuleOverride = catalogOv?.LibraryCapsule2x is not null;

                if (hasHeroOverride)
                {
                    heroCover = catalogOv!.LibraryHero2x!;
                }
                else
                {
                    var detail = await _storeService.GetDetailAsync(fix.AppId);
                    if (!hasCapsuleOverride && !string.IsNullOrWhiteSpace(detail?.LibraryCapsule2xUrl))
                    {
                        heroCover = detail.LibraryCapsule2xUrl;
                    }
                    if (!string.IsNullOrWhiteSpace(detail?.RawMetadataJson))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(detail.RawMetadataJson);
                            if (doc.RootElement.TryGetProperty("assets", out var assets) &&
                                assets.TryGetProperty("library_hero_2x", out var heroArr) &&
                                heroArr.ValueKind == System.Text.Json.JsonValueKind.Array)
                            {
                                foreach (var item in heroArr.EnumerateArray())
                                {
                                    if (item.TryGetProperty("url", out var urlProp) &&
                                        urlProp.ValueKind == System.Text.Json.JsonValueKind.String)
                                    {
                                        var heroUrl = urlProp.GetString();
                                        if (!string.IsNullOrWhiteSpace(heroUrl))
                                        {
                                            heroCover = heroUrl;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }

                results[idx] = new FixEntry
                {
                    AppId = fix.AppId,
                    Title = fix.Title,
                    Publisher = fix.Publisher,
                    Category = fix.Category,
                    PosterUrl = heroCover,
                    Password = fix.Password,
                    IsPremium = fix.IsPremium,
                    ExeHint = fix.ExeHint,
                    UseShortcut = fix.UseShortcut,
                    Files = fix.Files
                };
            }
            catch
            {
                results[idx] = fix;
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.ToList();
    }
}
