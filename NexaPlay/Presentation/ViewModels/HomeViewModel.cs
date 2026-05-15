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

    [ObservableProperty] private int _totalFixes;
    [ObservableProperty] private int _libraryCount;
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private IReadOnlyList<FixEntry> _recentFixes = Array.Empty<FixEntry>();
    [ObservableProperty] private ObservableCollection<GameEntry> _popularGames = new();
    [ObservableProperty] private FixEntry? _heroGame;

    private IReadOnlyList<int> _allPopularAppIds = Array.Empty<int>();
    private int _currentPopularPage = 0;
    private const int PopularGamesPageSize = 32;

    public HomeViewModel(IBypassGamesDataService fixData, IAddGameService addGame, IMetadataService metadata)
    {
        _fixData = fixData;
        _addGame = addGame;
        _metadata = metadata;
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
            var dispatcher = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
            
            await Task.Run(async () =>
            {
                _allPopularAppIds = await _metadata.GetPopularAppIdsAsync();
                _currentPopularPage = 0; // Using this as the current index tracker now
                
                var popularList = new List<GameEntry>();
                while(popularList.Count < PopularGamesPageSize && _currentPopularPage < _allPopularAppIds.Count)
                {
                    var pid = _allPopularAppIds[_currentPopularPage++];
                    var meta = await _metadata.GetMetadataAsync(pid);
                    if (meta != null) popularList.Add(meta);
                }
                
                if (dispatcher != null)
                {
                    dispatcher.TryEnqueue(() => 
                    {
                        PopularGames = new ObservableCollection<GameEntry>(popularList);
                    });
                }
                else
                {
                    PopularGames = new ObservableCollection<GameEntry>(popularList);
                }
            });
        }
        catch (Exception) { /* Ignored */ }
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
