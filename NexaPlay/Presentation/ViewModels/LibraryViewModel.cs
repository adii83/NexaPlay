using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Enums;
using NexaPlay.Core.Models;
using NexaPlay.Presentation.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed class LibraryGameCard
{
    public int AppId { get; init; }
    public string AppIdDisplay => $"AppID: {AppId}";
    public string Title { get; init; } = string.Empty;
    public string PosterUrl { get; init; } = string.Empty;
    public bool HasCover => !string.IsNullOrWhiteSpace(PosterUrl) && PosterUrl != "NO CONTENT";
    public bool FixApplied { get; init; }
    public string Genre { get; init; } = string.Empty;
    public bool IsPremium { get; init; }
    public bool HasDenuvo { get; init; }
    public bool IsStandard => !IsPremium;
}

public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IAddGameService _addGame;
    private readonly IOnlineFixService _onlineFix;
    private readonly ISteamService _steam;
    private readonly IAppLogService _log;
    private readonly IMetadataService _metadata;
    private readonly INavigationService _nav;
    private readonly INexaPlayOverrideService _nexaPlayOverride;
    private readonly ISteamStoreService _steamStore;

    private int PageSize => 10;
    private int _currentPage = 1;
    private IReadOnlyList<InstalledGame> _allInstalledGames = Array.Empty<InstalledGame>();
    private List<InstalledGame> _filteredGames = new();
    private CancellationTokenSource? _searchDebounceCts;

    [ObservableProperty] public partial ObservableCollection<LibraryGameCard> Games { get; set; }
    [ObservableProperty] public partial string SearchQuery { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial int TotalCount { get; set; }
    [ObservableProperty] public partial int FixedCount { get; set; }

    // Pagination
    [ObservableProperty] public partial string CurrentPageLabel { get; set; }
    [ObservableProperty] public partial bool CanGoNext { get; set; }
    [ObservableProperty] public partial bool CanGoPrevious { get; set; }
    [ObservableProperty] public partial int TotalPages { get; set; }
    [ObservableProperty] public partial int CurrentPage { get; set; }

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

    public bool IsEmpty => !IsLoading && Games.Count == 0 && string.IsNullOrWhiteSpace(SearchQuery);

    public LibraryViewModel(
        IAddGameService addGame,
        IOnlineFixService onlineFix,
        ISteamService steam,
        IAppLogService log,
        IMetadataService metadata,
        INavigationService nav,
        INexaPlayOverrideService nexaPlayOverride,
        ISteamStoreService steamStore)
    {
        _addGame = addGame;
        _onlineFix = onlineFix;
        _steam = steam;
        _log = log;
        _metadata = metadata;
        _nav = nav;
        _nexaPlayOverride = nexaPlayOverride;
        _steamStore = steamStore;

        Games = new ObservableCollection<LibraryGameCard>();
        SearchQuery = string.Empty;
        CurrentPageLabel = "Halaman 1";
        CurrentPage = 1;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var libraryAppIds = _addGame.ListLibraryGames();
            var list = new List<InstalledGame>();
            
            foreach (var appIdStr in libraryAppIds)
            {
                if (int.TryParse(appIdStr, out var appId))
                {
                    var meta = await _metadata.GetMetadataAsync(appId);
                    var steamName = _steam.GetGameName(appId);
                    
                    list.Add(new InstalledGame
                    {
                        AppId = appId,
                        Name = meta?.Name ?? steamName ?? $"App {appId}",
                        InstallPath = _steam.ResolveGameInstallPath(appId) ?? string.Empty,
                        FixApplied = _onlineFix.IsApplied(appId)
                    });
                }
            }
            
            _allInstalledGames = list;
            FixedCount = _allInstalledGames.Count(g => g.FixApplied);

            await ApplyFiltersAndPaginationAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
    private async Task ApplyFiltersAndPaginationAsync(bool resetPage = true)
    {
        IEnumerable<InstalledGame> query = _allInstalledGames;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var q = SearchQuery.Trim();
            if (int.TryParse(q, out var searchAppId))
            {
                query = query.Where(x => x.AppId == searchAppId);
            }
            else
            {
                var lowered = q.ToLowerInvariant();
                query = query.Where(x => (x.Name ?? "").ToLowerInvariant().Contains(lowered, StringComparison.Ordinal));
            }
        }

        _filteredGames = query.OrderBy(x => x.Name).ToList();
        TotalCount = _filteredGames.Count;

        if (resetPage)
        {
            _currentPage = 1;
        }

        var totalPages = Math.Max(1, (int)Math.Ceiling(_filteredGames.Count / (double)PageSize));
        TotalPages = totalPages;
        if (_currentPage > totalPages)
        {
            _currentPage = totalPages;
        }

        var skip = (_currentPage - 1) * PageSize;
        var pageItems = _filteredGames.Skip(skip).Take(PageSize).ToList();

        var targetCards = new List<LibraryGameCard>(pageItems.Count);
        foreach (var item in pageItems)
        {
            var meta = await _metadata.GetMetadataAsync(item.AppId);
            var overrideData = await _nexaPlayOverride.GetCatalogOverrideAsync(item.AppId, CancellationToken.None);
            var detail = await _steamStore.GetDetailAsync(item.AppId, CancellationToken.None);

            var title = meta?.Name ?? item.Name;
            var cover = overrideData?.LibraryCapsule ?? meta?.HeaderImageUrl ?? meta?.RawHeaderImageUrl ?? detail?.BackgroundImageUrl;

            if (string.IsNullOrWhiteSpace(cover))
                cover = "NO CONTENT";

            targetCards.Add(new LibraryGameCard
            {
                AppId = item.AppId,
                Title = title,
                PosterUrl = cover,
                FixApplied = item.FixApplied,
                Genre = meta?.Genre ?? "Unknown",
                IsPremium = meta?.IsPremium ?? false,
                HasDenuvo = meta?.HasDenuvo ?? false
            });
        }

        SyncGames(targetCards);

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
        OnPropertyChanged(nameof(IsEmpty));
    }

    private void SyncGames(IReadOnlyList<LibraryGameCard> targetItems)
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

    [RelayCommand]
    private Task SearchNow() => ApplyFiltersAndPaginationAsync();

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
            page = intPage;
        else if (pageParam is string pageText && int.TryParse(pageText, out var parsed))
            page = parsed;
        else
            return;

        if (page < 1 || page > TotalPages) return;
        _currentPage = page;
        _ = ApplyFiltersAndPaginationAsync(resetPage: false);
    }

    [RelayCommand]
    private void NavigateToGames()
    {
        _nav.Navigate<GamesPage>();
    }

    [RelayCommand]
    private async Task RemoveGameAsync(string appId)
    {
        await _addGame.RemoveGameAsync(appId);
        await LoadAsync();
    }

    [RelayCommand]
    private void RestartSteam()
    {
        _ = _steam.RestartAsync();
    }

    partial void OnSearchQueryChanged(string value) => DebounceSearch();
    private async void DebounceSearch()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        try
        {
            await Task.Delay(300, cts.Token);
            if (cts.IsCancellationRequested) return;
            await ApplyFiltersAndPaginationAsync();
        }
        catch (TaskCanceledException) { }
    }

    partial void OnGamesChanged(ObservableCollection<LibraryGameCard> value) => OnPropertyChanged(nameof(IsEmpty));
    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));
}
