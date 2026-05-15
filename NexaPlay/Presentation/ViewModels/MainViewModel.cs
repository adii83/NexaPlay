using CommunityToolkit.Mvvm.ComponentModel;

namespace NexaPlay.Presentation.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private string _currentPageTitle = "Dashboard";

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    public void SetCurrentPageTitle(string title)
    {
        CurrentPageTitle = title;
    }
}
