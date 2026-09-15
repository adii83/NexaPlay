using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Helpers;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class GamesViewModel : ObservableObject
{
    private const int RowsPerPage = 10;
    private const int MinimumSearchLength = 3;
    private static readonly SemaphoreSlim PrefetchGate = new(4, 4);

    private readonly ICatalogRepository _catalog;
    private readonly ICoverImageCacheService _coverImageCache;
    private readonly ICatalogRefreshState _catalogRefreshState;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<int, FixEntry> _cardCache = new();
    private readonly CatalogLoadCoordinator _catalogLoad = new();
    private CancellationTokenSource? _searchDebounceCts;
    private CancellationTokenSource? _applyCts;
    private CancellationTokenSource? _prefetchCts;
    private int _currentPage = 1;
    private int _gridColumns = 5;
    private bool _isUpdatingFilters;
    private DateOnly? _browseDate;

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
    [ObservableProperty] public partial IReadOnlyList<string> GenreMaster { get; set; }
    [ObservableProperty] public partial IReadOnlyList<string> SelectedGenres { get; set; }
    [ObservableProperty] public partial bool IsSearchHintVisible { get; set; }
    [ObservableProperty] public partial string SearchHintText { get; set; }

    private int PageSize => _gridColumns * RowsPerPage;
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
    public bool CanExecuteSearch
    {
        get
        {
            var search = SearchQuery?.Trim() ?? string.Empty;
            return search.Length == 0 || int.TryParse(search, out _) || search.Length >= MinimumSearchLength;
        }
    }
    public bool IsEmpty => !IsLoading && Games.Count == 0;

    public GamesViewModel(
        ICatalogRepository catalog,
        ICoverImageCacheService coverImageCache,
        ICatalogRefreshState catalogRefreshState,
        TimeProvider timeProvider)
    {
        _catalog = catalog;
        _coverImageCache = coverImageCache;
        _catalogRefreshState = catalogRefreshState;
        _timeProvider = timeProvider;

        Games = new ObservableCollection<FixEntry>();
        CurrentPageLabel = "Halaman 1";
        CurrentPage = 1;
        SelectedGenres = Array.Empty<string>();
        SearchQuery = string.Empty;
        SearchHintText = string.Empty;
        GenreMaster =
        [
            "Indie", "Action", "Casual", "Adventure", "RPG", "Strategy", "Sports", "Racing",
            "Massively Multiplayer", "Design & Illustration", "Web Publishing", "Utilities", "Education",
            "Game Development", "Simulation", "Violent", "Video Production", "Audio Production",
            "Software Training", "Gore", "Movie", "Photo Editing", "Sexual Content", "Nudity", "Episodic",
            "Tutorial", "Documentary", "Accounting"
        ];
    }

    public Task LoadAsync() => LoadAsync(false);
    public Task ReloadCatalogAsync()
    {
        BeginBrowseSession();
        return LoadAsync(true);
    }

    public void InvalidateDerivedCache()
    {
        _catalogLoad.Invalidate();
        InvalidateCatalogState();
    }

    public bool BeginBrowseSession()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        if (_browseDate == today)
            return false;

        _browseDate = today;
        _currentPage = 1;
        InvalidateDerivedCache();
        return true;
    }

    public void CancelPendingWork()
    {
        _searchDebounceCts?.Cancel();
        _applyCts?.Cancel();
        _prefetchCts?.Cancel();
    }

    private Task LoadAsync(bool forceReload) => _catalogLoad.RunAsync(
        () => _catalogRefreshState.Generation,
        forceReload,
        InvalidateCatalogState,
        _ => ApplyFiltersAndPaginationAsync(resetPage: false));

    private void InvalidateCatalogState()
    {
        _applyCts?.Cancel();
        _prefetchCts?.Cancel();
        _cardCache.Clear();
        Games.Clear();
    }

    public void UpdateGridColumns(int columns)
    {
        var normalized = Math.Clamp(columns, 3, 6);
        if (_gridColumns == normalized)
            return;

        _gridColumns = normalized;
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

        try
        {
            var page = await _catalog.QueryAsync(CreateQuery(requestPage), ct);
            TotalCount = page.TotalCount;
            var totalPages = Math.Max(1, (int)Math.Ceiling(page.TotalCount / (double)PageSize));
            _currentPage = Math.Min(requestPage, totalPages);

            if (_currentPage != requestPage)
                page = await _catalog.QueryAsync(CreateQuery(_currentPage), ct);

            var targetPageItems = await BuildPageItemsAsync(page.Items, ct);
            ct.ThrowIfCancellationRequested();
            SyncGamesPageItems(targetPageItems);

            TotalPages = totalPages;
            CurrentPageLabel = $"Halaman {_currentPage}";
            CurrentPage = _currentPage;
            CanGoPrevious = _currentPage > 1;
            CanGoNext = _currentPage < totalPages;
            RaisePagerProperties();
            _ = PreFetchNextPagesBackgroundAsync(_currentPage);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_applyCts?.Token == ct)
                IsLoading = false;
        }
    }

    private CatalogQuery CreateQuery(int page, int? limit = null)
    {
        var search = SearchQuery?.Trim() ?? string.Empty;
        if (search.Length is > 0 and < MinimumSearchLength && !int.TryParse(search, out _))
            search = string.Empty;

        bool? premium = FilterPremium && !FilterStandard
            ? true
            : FilterStandard && !FilterPremium ? false : null;
        bool? protection = FilterDenuvo && !FilterNonDenuvo
            ? true
            : FilterNonDenuvo && !FilterDenuvo ? false : null;

        var browseDate = _browseDate ?? DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        _browseDate ??= browseDate;
        return new CatalogQuery(
            search,
            premium,
            protection,
            ExpandGenreAliases(SelectedGenres),
            string.IsNullOrWhiteSpace(search),
            (page - 1) * PageSize,
            limit ?? PageSize,
            (browseDate.Year * 10_000) + (browseDate.Month * 100) + browseDate.Day);
    }

    private async Task<IReadOnlyList<FixEntry>> BuildPageItemsAsync(IReadOnlyList<GameEntry> games, CancellationToken ct)
    {
        var results = new FixEntry?[games.Count];
        using var gate = new SemaphoreSlim(4, 4);
        var tasks = games.Select(async (game, index) =>
        {
            await gate.WaitAsync(ct);
            try
            {
                results[index] = await GetOrBuildListingCardAsync(game, ct);
            }
            catch
            {
                results[index] = BuildFastCard(game);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.Where(item => item is not null).Select(item => item!).ToList();
    }

    private FixEntry BuildFastCard(GameEntry game)
    {
        if (_cardCache.TryGetValue(game.AppId, out var cached))
            return cached;

        var card = new FixEntry
        {
            AppId = game.AppId,
            Title = game.Name,
            Publisher = game.PublisherDisplay,
            PosterUrl = FirstNonEmpty(game.PopularCoverImageUrl, game.RawHeaderImageUrl) ?? "NO CONTENT",
            IsPremium = game.IsPremium,
            HasDenuvo = game.HasDenuvo,
            Category = ParseCategory(game.Genre)
        };
        _cardCache[game.AppId] = card;
        return card;
    }

    private async Task<FixEntry> GetOrBuildListingCardAsync(GameEntry game, CancellationToken ct)
    {
        var card = BuildFastCard(game);
        var cover = FirstNonEmpty(game.LibraryCapsuleUrl, game.RawHeaderImageUrl);
        cover = string.IsNullOrWhiteSpace(cover)
            ? "NO CONTENT"
            : await _coverImageCache.GetCachedOrFetchAsync(game.AppId, cover, ct) ?? cover;

        if (string.Equals(card.PosterUrl, cover, StringComparison.OrdinalIgnoreCase))
            return card;

        var updated = new FixEntry
        {
            AppId = card.AppId,
            Title = card.Title,
            Publisher = card.Publisher,
            PosterUrl = cover,
            IsPremium = card.IsPremium,
            HasDenuvo = card.HasDenuvo,
            Category = card.Category
        };
        _cardCache[game.AppId] = updated;
        return updated;
    }

    private async Task PreFetchNextPagesBackgroundAsync(int startPage)
    {
        _prefetchCts?.Cancel();
        _prefetchCts?.Dispose();
        _prefetchCts = new CancellationTokenSource();
        var ct = _prefetchCts.Token;

        try
        {
            var query = CreateQuery(startPage + 1);
            var page = await _catalog.QueryAsync(query, ct);
            var tasks = page.Items.Select(async game =>
            {
                await PrefetchGate.WaitAsync(ct);
                try { await GetOrBuildListingCardAsync(game, ct); }
                catch { }
                finally { PrefetchGate.Release(); }
            });
            await Task.WhenAll(tasks);
        }
        catch
        {
        }
    }

    [RelayCommand]
    private Task SearchNow()
    {
        UpdateSearchHintState(SearchQuery);
        return CanExecuteSearch ? ApplyFiltersAndPaginationAsync() : Task.CompletedTask;
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
        var valid = pageParam switch
        {
            int value => value,
            string text when int.TryParse(text, out var value) => value,
            _ => 0
        };
        if (valid < 1 || valid > TotalPages) return;
        _currentPage = valid;
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
        var changed = isIncluded ? set.Add(genre) : set.Remove(genre);
        if (!changed) return;
        SelectedGenres = set.ToList();
        _ = ApplyFiltersAndPaginationAsync();
    }

    partial void OnSearchQueryChanged(string value)
    {
        _applyCts?.Cancel();
        _prefetchCts?.Cancel();
        UpdateSearchHintState(value);
        OnPropertyChanged(nameof(CanExecuteSearch));
        if (!CanExecuteSearch)
        {
            Games.Clear();
            TotalCount = 0;
            TotalPages = 1;
            _currentPage = 1;
            CurrentPage = 1;
            CurrentPageLabel = "Halaman 1";
            CanGoPrevious = false;
            CanGoNext = false;
            RaisePagerProperties();
            return;
        }
        DebounceSearch();
    }

    partial void OnFilterStandardChanged(bool value) => UpdateExclusiveFilter(value, () => FilterPremium = false);
    partial void OnFilterPremiumChanged(bool value) => UpdateExclusiveFilter(value, () => FilterStandard = false);
    partial void OnFilterDenuvoChanged(bool value) => UpdateExclusiveFilter(value, () => FilterNonDenuvo = false);
    partial void OnFilterNonDenuvoChanged(bool value) => UpdateExclusiveFilter(value, () => FilterDenuvo = false);
    partial void OnGamesChanged(ObservableCollection<FixEntry> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    private void UpdateExclusiveFilter(bool value, Action clearOther)
    {
        if (_isUpdatingFilters) return;
        if (value)
        {
            _isUpdatingFilters = true;
            clearOther();
            _isUpdatingFilters = false;
        }
        _ = ApplyFiltersAndPaginationAsync();
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
            UpdateSearchHintState(SearchQuery);
            if (CanExecuteSearch)
                await ApplyFiltersAndPaginationAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private void UpdateSearchHintState(string? value)
    {
        var search = value?.Trim() ?? string.Empty;
        IsSearchHintVisible = search.Length is > 0 and < MinimumSearchLength && !int.TryParse(search, out _);
        SearchHintText = IsSearchHintVisible ? $"Masukkan minimal {MinimumSearchLength} karakter." : string.Empty;
    }

    private void SyncGamesPageItems(IReadOnlyList<FixEntry> targetItems)
    {
        while (Games.Count > targetItems.Count)
            Games.RemoveAt(Games.Count - 1);
        for (var index = 0; index < targetItems.Count; index++)
        {
            if (index < Games.Count)
                Games[index] = targetItems[index];
            else
                Games.Add(targetItems[index]);
        }
    }

    private void RaisePagerProperties()
    {
        OnPropertyChanged(nameof(TotalPagesLabel));
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

    private static IReadOnlyList<string> ExpandGenreAliases(IReadOnlyList<string>? genres)
    {
        if (genres is null || genres.Count == 0)
            return Array.Empty<string>();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var genre in genres)
        {
            result.Add(genre);
            if (GenreAliasMap.TryGetValue(genre, out var aliases))
            {
                foreach (var alias in aliases)
                    result.Add(alias);
            }
        }
        return result.ToList();
    }

    private static GameCategory ParseCategory(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre)) return GameCategory.Other;
        foreach (GameCategory category in Enum.GetValues(typeof(GameCategory)))
        {
            if (category != GameCategory.Other && genre.Contains(category.ToString(), StringComparison.OrdinalIgnoreCase))
                return category;
        }
        return GameCategory.Other;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static readonly Dictionary<string, string[]> GenreAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Role-Playing"] = ["RPG"],
        ["RPG"] = ["Role-Playing"],
        ["Massively Multiplayer"] = ["MMO", "MMORPG"],
        ["MMO"] = ["Massively Multiplayer", "MMORPG"],
        ["Sports"] = ["Sports Game"],
        ["Racing"] = ["Racing Game"],
        ["Simulation"] = ["Simulation Game"],
        ["Indie"] = ["Indie Game"],
        ["Adventure"] = ["Adventure Game"],
        ["Action"] = ["Action Game"],
        ["Strategy"] = ["Strategy Game"],
        ["Casual"] = ["Casual Game"]
    };
}
