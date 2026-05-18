using System.ComponentModel;

namespace NexaPlay.Core.Models;

/// <summary>A single screenshot from Steam Store API appdetails.</summary>
public sealed class ScreenshotEntry : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Id { get; init; }

    /// <summary>600x338 thumbnail URL.</summary>
    public string ThumbnailUrl { get; init; } = string.Empty;

    /// <summary>1920x1080 full-resolution URL.</summary>
    public string FullUrl { get; init; } = string.Empty;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }
}
