using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using NexaPlay.Core.Helpers;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private readonly IMetadataService _metadata;
    private readonly IListingCoverResolver _listingCoverResolver;
    private readonly ICoverImageCacheService _coverImageCache;
    private readonly ICatalogRefreshState _catalogRefreshState;
    private const int RowsPerPage = 10;
    private const int MinimumSearchLength = 3;

    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial ObservableCollection<FixEntry> Games { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }
    [ObservableProperty] public partial bool IsFilterOpen { get; set; }
    [ObservableProperty] public partial bool FilterStandard { get; set; }
    [ObservableProperty] public partial bool FilterPremium { get; set; }
    [ObservableProperty] public partial bool FilterDenuvo { get; set; }
    [ObservableProperty] public partial bool FilterNonDenuvo { get; set; }
    [ObservableProperty] public partial string CurrentPageLabel { get; set; }
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    [ObservableProperty] public partial bool CanGoPrevious { get; set; }
    [ObservableProperty] public partial int TotalPages { get; set; }
    [ObservableProperty] public partial int CurrentPage { get; set; }
    public string TotalPagesLabel => $"/ {TotalPages}";
    public bool ShowPager => TotalPages > 1;
    public int PageSlot1 => TotalPages <= 3 ? 1 : Math.Clamp(CurrentPage - 1, 1, TotalPages - 2);
    public int PageSlot2 => TotalPages <= 3 ? Math.Min(2, TotalPages) : PageSlot1 + 1;
    public int PageSlot3 => TotalPages <= 3 ? Math.Min(3, TotalPages) : PageSlot1 + 2;
    public bool IsPage1Selected => CurrentPage == PageSlot1;
    public bool IsPage2Selected => CurrentPage == PageSlot2;
    public bool IsPage3Selected => CurrentPage == PageSlot3;
    public bool ShowPage1 => TotalPages >= 1;
    public bool ShowPage2 => TotalPages >= 2;
    public bool ShowPage3 => TotalPages >= 3;
    [ObservableProperty] public partial IReadOnlyList<string> GenreMaster { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> SelectedGenres { get; set; }
    [ObservableProperty] public partial bool IsSearchHintVisible { get; set; }
    [ObservableProperty] public partial string SearchHintText { get; set; }
    public bool CanExecuteSearch => string.IsNullOrWhiteSpace(SearchQuery) || SearchQuery.Trim().Length >= MinimumSearchLength;

    private IReadOnlyList<GameFilterIndex> _allFilterIndex = Array.Empty<GameFilterIndex>();
    private List<int> _filteredAppIds = new();
    private int _currentPage = 1;
    private int _gridColumns = 5;
    private bool _isUpdatingFilters;
    private int PageSize => _gridColumns * RowsPerPage;
    private readonly Dictionary<int, FixEntry> _cardCache = new();
    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _applyCts;
    private CancellationTokenSource? _prefetchCts;
    private static readonly SemaphoreSlim _prefetchGate = new SemaphoreSlim(4, 4);
    private readonly CatalogLoadCoordinator _catalogLoad = new();
    private const int FilterIndexCacheSchema = 1;
    private readonly string _catalogSourceDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NexaPlay",
        "runtime_catalog_sources");
    private readonly string _gamesIndexCachePath;

    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(
        IMetadataService metadata,
        IListingCoverResolver listingCoverResolver,
        ICoverImageCacheService coverImageCache,
        ICatalogRefreshState catalogRefreshState)
    {
        _metadata = metadata;
        _listingCoverResolver = listingCoverResolver;
        _coverImageCache = coverImageCache;
        _catalogRefreshState = catalogRefreshState;
        _gamesIndexCachePath = Path.Combine(_catalogSourceDirectory, "games_filter_index_cache_v3.json");

        // Default values for partial properties
        Games = new ObservableCollection<FixEntry>();
        CurrentPageLabel = "Halaman 1";
        CurrentPage = 1;
        SelectedGenres = Array.Empty<string>();
        SearchQuery = string.Empty;
        SearchHintText = string.Empty;
        GenreMaster = new[]
        {
            "Indie","Action","Casual","Adventure","RPG","Strategy","Sports","Racing",
            "Massively Multiplayer","Design & Illustration","Web Publishing","Utilities","Education",
            "Game Development","Simulation","Violent","Video Production","Audio Production",
            "Software Training","Gore","Movie","Photo Editing","Sexual Content","Nudity","Episodic",
            "Tutorial","Documentary","Accounting"
        };
    }

    public Task LoadAsync() => LoadAsync(forceReload: false);

    public Task ReloadCatalogAsync() => LoadAsync(forceReload: true);

    public void InvalidateDerivedCache()
    {
        InvalidateCatalogState();
        try { File.Delete(_gamesIndexCachePath); } catch { }
    }

    private Task LoadAsync(bool forceReload) => _catalogLoad.RunAsync(
        () => _catalogRefreshState.Generation,
        forceReload,
        InvalidateCatalogState,
        LoadCoreAsync);

    private async Task LoadCoreAsync(long generation)
    {
        IsLoading = true;
        try
        {
            _allFilterIndex = await LoadOrBuildFilterIndexAsync();

            TotalCount = _allFilterIndex.Count;
            await ApplyFiltersAndPaginationAsync(resetPage: false);
        }
        finally { IsLoading = false; }
    }

    private void InvalidateCatalogState()
    {
        _applyCts?.Cancel();
        _prefetchCts?.Cancel();
        _allFilterIndex = Array.Empty<GameFilterIndex>();
        _filteredAppIds.Clear();
        _cardCache.Clear();
    }

    private async Task<IReadOnlyList<GameFilterIndex>> LoadOrBuildFilterIndexAsync()
    {
        var sourceRevision = GetCatalogSourceRevision();
        var cached = await TryReadFilterIndexCacheAsync(sourceRevision);
        if (cached.Count > 0)
        {
            return cached;
        }

        var snapshot = await _metadata.GetCatalogSnapshotAsync();
        var built = snapshot
            .Select(game => new GameFilterIndex(
                game.AppId,
                (game.Name ?? string.Empty).NormalizeForSearch(),
                game.PriceNormalized,
                game.IsPremium,
                game.Protection,
                ParseGenreTokens(game.Genre)))
            .ToList();

        if (string.Equals(sourceRevision, GetCatalogSourceRevision(), StringComparison.Ordinal))
        {
            await TryWriteFilterIndexCacheAsync(built, sourceRevision);
        }

        return built;
    }

    public void UpdateGridColumns(int columns)
    {
        var normalized = Math.Clamp(columns, 3, 6);
        if (_gridColumns == normalized)
        {
            return;
        }

        var previousPage = _currentPage;
        _gridColumns = normalized;
        _currentPage = previousPage;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    private async Task ApplyFiltersAndPaginationAsync(bool resetPage = true)
    {
        _applyCts?.Cancel();
        _applyCts?.Dispose();
        _applyCts = new CancellationTokenSource();
        var ct = _applyCts.Token;
        var requestPage = resetPage ? 1 : _currentPage;
        IsLoading = true;

        if (SelectedGenres is null)
        {
            SelectedGenres = Array.Empty<string>();
        }

        try
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            _filteredAppIds = await BuildFilteredAppIdsAsync(ct);
            ct.ThrowIfCancellationRequested();
            TotalCount = _filteredAppIds.Count;
            _currentPage = requestPage;

            var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredAppIds.Count / (double)PageSize));
            TotalPages = totalPages;
            OnPropertyChanged(nameof(TotalPagesLabel));
            if (_currentPage > totalPages)
            {
                _currentPage = totalPages;
            }

            var skip = (_currentPage - 1) * PageSize;
            var pageIds = _filteredAppIds.Skip(skip).Take(PageSize).ToList();
            var targetPageItems = await BuildPageItemsAsync(pageIds, ct);
            ct.ThrowIfCancellationRequested();
            SyncGamesPageItems(targetPageItems);

            CurrentPageLabel = $"Halaman {_currentPage}";
            CurrentPage = _currentPage;
            CanGoPrevious = _currentPage > 1;
            CanGoNext = _currentPage < totalPages;
            OnPropertyChanged(nameof(ShowPager));
            OnPropertyChanged(nameof(PageSlot1));
            OnPropertyChanged(nameof(PageSlot2));
            OnPropertyChanged(nameof(PageSlot3));
            OnPropertyChanged(nameof(ShowPage1));
            OnPropertyChanged(nameof(ShowPage2));
            OnPropertyChanged(nameof(ShowPage3));
            OnPropertyChanged(nameof(IsPage1Selected));
            OnPropertyChanged(nameof(IsPage2Selected));
            OnPropertyChanged(nameof(IsPage3Selected));
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_applyCts?.Token == ct)
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private Task SearchNow()
    {
        UpdateSearchHintState(SearchQuery);
        if (!CanExecuteSearch)
        {
            return Task.CompletedTask;
        }

        return ApplyFiltersAndPaginationAsync();
    }

    [RelayCommand]
    private void NextPage()
    {
        if (!CanGoNext) return;
        _currentPage++;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void PreviousPage()
    {
        if (!CanGoPrevious) return;
        _currentPage--;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void GoToPage(object? pageParam)
    {
        int page;
        if (pageParam is int intPage)
        {
            page = intPage;
        }
        else if (pageParam is string pageText && int.TryParse(pageText, out var parsed))
        {
            page = parsed;
        }
        else
        {
            return;
        }

        if (page < 1 || page > TotalPages) return;
        _currentPage = page;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void ToggleFilter() => IsFilterOpen = !IsFilterOpen;

    [RelayCommand]
    private void ClearFilters()
    {
        _isUpdatingFilters = true;
        FilterStandard = false;
        FilterPremium = false;
        FilterDenuvo = false;
        FilterNonDenuvo = false;
        _isUpdatingFilters = false;
        SelectedGenres = Array.Empty<string>();
        SearchQuery = string.Empty;
        _ = ApplyFiltersAndPaginationAsync();
    }

    public void SetGenreFilter(string genre, bool isIncluded)
    {
        var set = SelectedGenres.ToHashSet(StringComparer.OrdinalIgnoreCase);
        bool changed = false;
        
        if (isIncluded)
            changed = set.Add(genre);
        else
            changed = set.Remove(genre);

        if (changed)
        {
            SelectedGenres = set.ToList();
            _ = ApplyFiltersAndPaginationAsync();
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        UpdateSearchHintState(value);
        OnPropertyChanged(nameof(CanExecuteSearch));
        DebounceSearch();
    }
    partial void OnFilterStandardChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterPremium = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterPremiumChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterStandard = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterDenuvoChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterNonDenuvo = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }
    
    partial void OnFilterNonDenuvoChanged(bool value) 
    {
        if (_isUpdatingFilters) return;
        if (value) { _isUpdatingFilters = true; FilterDenuvo = false; _isUpdatingFilters = false; }
        _ = ApplyFiltersAndPaginationAsync();
    }

    partial void OnGamesChanged(ObservableCollection<FixEntry> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    private void SyncGamesPageItems(IReadOnlyList<FixEntry> targetItems)
    {
        while (Games.Count > targetItems.Count)
        {
            Games.RemoveAt(Games.Count - 1);
        }

        for (var index = 0; index < targetItems.Count; index++)
        {
            if (index < Games.Count)
            {
                if (!ReferenceEquals(Games[index], targetItems[index]))
                {
                    Games[index] = targetItems[index];
                }
            }
            else
            {
                Games.Add(targetItems[index]);
            }
        }
    }

    private async Task<IReadOnlyList<FixEntry>> BuildPageItemsAsync(IReadOnlyList<int> pageIds, CancellationToken ct)
    {
        var results = new FixEntry?[pageIds.Count];
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = pageIds.Select(async (appId, idx) =>
        {
            await gate.WaitAsync(ct);
            try
            {
                ct.ThrowIfCancellationRequested();
                results[idx] = await GetOrBuildListingCardAsync(appId, ct);
            }
            catch
            {
                results[idx] = await GetOrBuildCardFastAsync(appId, ct);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        _ = PreFetchNextPagesBackgroundAsync(_currentPage);
        return results.Where(x => x is not null).Select(x => x!).ToList();
    }

    private async Task PreFetchNextPagesBackgroundAsync(int startPage)
    {
        _prefetchCts?.Cancel();
        _prefetchCts?.Dispose();
        _prefetchCts = new CancellationTokenSource();
        var ct = _prefetchCts.Token;

        try
        {
            var skip = startPage * PageSize;
            var targetCount = PageSize * 2;
            var targetIds = _filteredAppIds.Skip(skip).Take(targetCount).ToList();
            if (targetIds.Count == 0) return;

            var tasks = targetIds.Select(async appId =>
            {
                await _prefetchGate.WaitAsync(ct);
                try
                {
                    ct.ThrowIfCancellationRequested();
                    await GetOrBuildListingCardAsync(appId, ct);
                }
                catch { }
                finally { _prefetchGate.Release(); }
            });
            await Task.WhenAll(tasks);
        }
        catch { }
    }

    private async Task<FixEntry?> GetOrBuildCardFastAsync(int appId, CancellationToken ct = default)
    {
        if (_cardCache.TryGetValue(appId, out var cached))
        {
            return cached;
        }

        var metadata = await _metadata.GetMetadataAsync(appId, ct);
        if (metadata is null)
        {
            return null;
        }

        var selectedCover = FirstNonEmpty(
            metadata.PopularCoverImageUrl,
            metadata.HeaderImageUrl,
            metadata.RawHeaderImageUrl,
            null);

        if (string.IsNullOrWhiteSpace(selectedCover))
        {
            selectedCover = "NO CONTENT";
        }

        var card = new FixEntry
        {
            AppId = metadata.AppId,
            Title = metadata.Name,
            Publisher = metadata.PublisherDisplay,
            PosterUrl = selectedCover,
            IsPremium = metadata.IsPremium,
            HasDenuvo = metadata.HasDenuvo,
            Category = ParseCategory(metadata.Genre)
        };

        _cardCache[appId] = card;
        return card;
    }

    private async Task<FixEntry?> GetOrBuildListingCardAsync(int appId, CancellationToken ct)
    {
        var card = await GetOrBuildCardFastAsync(appId, ct);
        if (card is null)
        {
            return null;
        }

        var metadata = await _metadata.GetMetadataAsync(appId, ct);
        if (metadata is null)
        {
            return card;
        }

        var selectedCover = await _listingCoverResolver.ResolveAsync(
            appId,
            metadata.LibraryCapsuleUrl,
            metadata.RawHeaderImageUrl,
            ct);

        if (string.IsNullOrWhiteSpace(selectedCover))
        {
            selectedCover = "NO CONTENT";
        }
        else
        {
            selectedCover = await _coverImageCache.GetCachedOrFetchAsync(appId, selectedCover, ct) ?? selectedCover;
        }

        if (string.Equals(card.PosterUrl, selectedCover, StringComparison.OrdinalIgnoreCase))
        {
            return card;
        }

        var updatedCard = new FixEntry
        {
            AppId = card.AppId,
            Title = card.Title,
            Publisher = card.Publisher,
            PosterUrl = selectedCover,
            IsPremium = card.IsPremium,
            HasDenuvo = card.HasDenuvo,
            Category = card.Category
        };

        _cardCache[appId] = updatedCard;
        return updatedCard;
    }

    private async Task<List<int>> BuildFilteredAppIdsAsync(CancellationToken ct)
    {
        var localSearchQuery = SearchQuery?.Trim() ?? string.Empty;
        var localFilterStandard = FilterStandard;
        var localFilterPremium = FilterPremium;
        var localFilterDenuvo = FilterDenuvo;
        var localFilterNonDenuvo = FilterNonDenuvo;
        var localSelectedGenres = SelectedGenres
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToHashSet(StringComparer.Ordinal);

        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            IEnumerable<GameFilterIndex> query = _allFilterIndex;

            if (!string.IsNullOrWhiteSpace(localSearchQuery) && localSearchQuery.Length >= MinimumSearchLength)
            {
                if (int.TryParse(localSearchQuery, out var searchAppId))
                {
                    query = query.Where(x => x.AppId == searchAppId);
                }
                else
                {
                    var lowered = localSearchQuery.NormalizeForSearch();
                    query = query.Where(x => x.NameLower.Contains(lowered, StringComparison.Ordinal));
                }
            }

            if (localFilterPremium && !localFilterStandard)
            {
                query = query.Where(x => x.IsPremium);
            }
            else if (localFilterStandard && !localFilterPremium)
            {
                query = query.Where(x => !x.IsPremium);
            }

            if (localFilterDenuvo && !localFilterNonDenuvo)
            {
                query = query.Where(x => x.HasDenuvo);
            }
            else if (localFilterNonDenuvo && !localFilterDenuvo)
            {
                query = query.Where(x => !x.HasDenuvo);
            }

            if (localSelectedGenres.Count > 0)
            {
                query = query.Where(x => x.GenreTokens.Overlaps(localSelectedGenres));
            }

            if (string.IsNullOrWhiteSpace(localSearchQuery))
            {
                query = query.Where(x => x.PriceNormalized >= 100000);
            }

            var daysSinceEpoch = (DateTime.UtcNow - DateTime.UnixEpoch).TotalDays;
            var seed = (int)(daysSinceEpoch / 2);
            var random = new Random(seed);

            ct.ThrowIfCancellationRequested();
            return query.OrderBy(x => random.Next()).Select(x => x.AppId).ToList();
        }, ct);
    }

    private void UpdateSearchHintState(string? value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        var shouldShowHint = trimmed.Length > 0 && trimmed.Length < MinimumSearchLength;

        IsSearchHintVisible = shouldShowHint;
        SearchHintText = shouldShowHint
            ? $"Masukkan minimal {MinimumSearchLength} karakter."
            : string.Empty;
    }

    private static GameCategory ParseCategory(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return GameCategory.Other;
        }

        foreach (GameCategory category in Enum.GetValues(typeof(GameCategory)))
        {
            if (category == GameCategory.Other)
            {
                continue;
            }

            if (genre.Contains(category.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }

        return GameCategory.Other;
    }

    private static HashSet<string> ParseGenreTokens(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var baseTokens = genre
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToLowerInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);

        var expanded = new HashSet<string>(baseTokens, StringComparer.Ordinal);
        foreach (var token in baseTokens)
        {
            if (GenreAliasMap.TryGetValue(token, out var aliases))
            {
                foreach (var alias in aliases)
                {
                    expanded.Add(alias);
                }
            }
        }

        return expanded;
    }

    private async void DebounceSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();

        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        try
        {
            await Task.Delay(220, cts.Token);
            if (cts.IsCancellationRequested)
            {
                return;
            }

            UpdateSearchHintState(SearchQuery);
            if (!string.IsNullOrWhiteSpace(SearchQuery) && !CanExecuteSearch)
            {
                return;
            }

            await ApplyFiltersAndPaginationAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private sealed record GameFilterIndex(
        int AppId,
        string NameLower,
        int PriceNormalized,
        bool IsPremium,
        bool HasDenuvo,
        HashSet<string> GenreTokens);

    private sealed record GameFilterIndexCacheItem(
        int AppId,
        string NameLower,
        int PriceNormalized,
        bool IsPremium,
        bool HasDenuvo,
        string[] GenreTokens);

    private string GetCatalogSourceRevision()
    {
        string[] sourceNames =
        [
            "fix_games.json",
            "new_fix_games.json",
            "new_games_catalog.json",
            "nexaplay_override.json",
            "override_data.json",
            "steam_data.json",
            "steam_data.json.gz",
            "steam_games.json"
        ];
        var stamps = new List<CatalogSourceStamp>(sourceNames.Length);

        foreach (var sourceName in sourceNames)
        {
            try
            {
                var file = new FileInfo(Path.Combine(_catalogSourceDirectory, sourceName));
                if (file.Exists)
                {
                    stamps.Add(new CatalogSourceStamp(file.Name, file.Length, file.LastWriteTimeUtc.Ticks));
                }
            }
            catch
            {
            }
        }

        return CatalogCacheStamp.CreateRevision(stamps.ToArray());
    }

    private async Task<IReadOnlyList<GameFilterIndex>> TryReadFilterIndexCacheAsync(string sourceRevision)
    {
        try
        {
            if (!File.Exists(_gamesIndexCachePath))
            {
                return Array.Empty<GameFilterIndex>();
            }

            await using var fs = File.OpenRead(_gamesIndexCachePath);
            var cached = await JsonSerializer.DeserializeAsync<CatalogCacheEnvelope<List<GameFilterIndexCacheItem>>>(fs);
            if (!CatalogCacheStamp.IsCurrent(cached, FilterIndexCacheSchema, sourceRevision) ||
                cached!.Items.Count == 0)
            {
                return Array.Empty<GameFilterIndex>();
            }

            return cached.Items.Select(x => new GameFilterIndex(
                x.AppId,
                x.NameLower ?? string.Empty,
                x.PriceNormalized,
                x.IsPremium,
                x.HasDenuvo,
                (x.GenreTokens ?? Array.Empty<string>()).ToHashSet(StringComparer.Ordinal)))
                .ToList();
        }
        catch
        {
            return Array.Empty<GameFilterIndex>();
        }
    }

    private async Task TryWriteFilterIndexCacheAsync(IReadOnlyList<GameFilterIndex> source, string sourceRevision)
    {
        var tempPath = $"{_gamesIndexCachePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            Directory.CreateDirectory(_catalogSourceDirectory);
            var items = source.Select(x => new GameFilterIndexCacheItem(
                x.AppId,
                x.NameLower,
                x.PriceNormalized,
                x.IsPremium,
                x.HasDenuvo,
                x.GenreTokens.ToArray()))
                .ToList();
            var payload = new CatalogCacheEnvelope<List<GameFilterIndexCacheItem>>(
                FilterIndexCacheSchema,
                sourceRevision,
                items);

            await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(fs, payload);
                await fs.FlushAsync();
            }

            File.Move(tempPath, _gamesIndexCachePath, overwrite: true);
        }
        catch
        {
        }
        finally
        {
            try
            {
                File.Delete(tempPath);
            }
            catch
            {
            }
        }
    }

    private static string? FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static readonly Dictionary<string, string[]> GenreAliasMap = new(StringComparer.Ordinal)
    {
        ["role-playing"] = ["rpg"],
        ["rpg"] = ["role-playing"],
        ["massively multiplayer"] = ["mmo"],
        ["mmo"] = ["massively multiplayer"],
        ["sports game"] = ["sports"],
        ["racing game"] = ["racing"],
        ["simulation game"] = ["simulation"],
        ["indie game"] = ["indie"],
        ["adventure game"] = ["adventure"],
        ["action game"] = ["action"],
        ["strategy game"] = ["strategy"],
        ["casual game"] = ["casual"],
        ["mmorpg"] = ["mmo", "rpg", "massively multiplayer"]
    };
}


