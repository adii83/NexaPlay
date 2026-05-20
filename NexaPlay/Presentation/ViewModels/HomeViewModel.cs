using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexaPlay.Contracts.Services;
using NexaPlay.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NexaPlay.Presentation.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    private readonly IBypassGamesDataService _fixData;
    private readonly IAddGameService _addGame;
    private readonly IMetadataService _metadata;
    private readonly IAppLogService _log;

    [ObservableProperty] public partial int TotalFixes { get; set; }
    [ObservableProperty] public partial int LibraryCount { get; set; }
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial IReadOnlyList<FixEntry> RecentFixes { get; set; }
    [ObservableProperty] public partial ObservableCollection<GameEntry> PopularGames { get; set; }
    [ObservableProperty] public partial FixEntry? HeroGame { get; set; }

    private IReadOnlyList<int> _allPopularAppIds = Array.Empty<int>();
    private int _currentPopularPage = 0;
    private const int PopularGamesPageSize = 32;

    public HomeViewModel(
        IBypassGamesDataService fixData,
        IAddGameService addGame,
        IMetadataService metadata,
        IAppLogService log)
    {
        _fixData = fixData;
        _addGame = addGame;
        _metadata = metadata;
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
            
            if (fixes.Count > 0)
            {
                HeroGame = fixes.FirstOrDefault(f => f.PosterUrl != null) ?? fixes.FirstOrDefault(); 
                RecentFixes = fixes.Take(12).ToList();
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
        try
        {
            _allPopularAppIds = await _metadata.GetPopularAppIdsAsync();
            _currentPopularPage = 0;
            _log.Log("Home", $"Popular app ids loaded: {_allPopularAppIds.Count}");

            var popularList = new List<GameEntry>();
            while (popularList.Count < PopularGamesPageSize && _currentPopularPage < _allPopularAppIds.Count)
            {
                var pid = _allPopularAppIds[_currentPopularPage++];
                var meta = await _metadata.GetMetadataAsync(pid);
                if (meta != null) popularList.Add(meta);
            }

            PopularGames = new ObservableCollection<GameEntry>(popularList);
            _log.Log("Home", $"Popular games rendered: {PopularGames.Count}");
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

        var newGames = new List<GameEntry>();
        while(newGames.Count < PopularGamesPageSize && _currentPopularPage < _allPopularAppIds.Count)
        {
            var pid = _allPopularAppIds[_currentPopularPage++];
            var meta = await _metadata.GetMetadataAsync(pid);
            if (meta != null) newGames.Add(meta);
        }

        foreach(var g in newGames)
        {
            PopularGames.Add(g);
        }
    }
}
