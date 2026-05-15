using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NexaPlay.Contracts.Navigation;
using NexaPlay.Presentation.Views.Pages;
using System;

namespace NexaPlay.Presentation.Navigation;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame) => _frame = frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public bool Navigate<T>(object? parameter = null) where T : Microsoft.UI.Xaml.Controls.Page
        => Navigate(typeof(T), parameter);

    public bool Navigate(Type pageType, object? parameter = null)
    {
        if (_frame is null) return false;
        if (_frame.CurrentSourcePageType == pageType) return true;
        return _frame.Navigate(pageType, parameter, new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromRight
        });
    }

    public void GoBack() => _frame?.GoBack();
}
