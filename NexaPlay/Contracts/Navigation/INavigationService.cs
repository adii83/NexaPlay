using Microsoft.UI.Xaml.Controls;
using System;

namespace NexaPlay.Contracts.Navigation;

public interface INavigationService
{
    void Initialize(Frame frame);
    bool Navigate<T>(object? parameter = null) where T : Microsoft.UI.Xaml.Controls.Page;
    bool Navigate(Type pageType, object? parameter = null);
    bool CanGoBack { get; }
    void GoBack();
}
