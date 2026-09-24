using grzyClothTool.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace grzyClothTool.Helpers;

public class NavigationHelper : INotifyPropertyChanged
{
    private readonly Dictionary<string, Func<UserControl>> _pageFactories = [];
    private readonly Dictionary<string, UserControl> _pages = [];

    public event PropertyChangedEventHandler PropertyChanged;

    private UserControl _currentPage;
    public UserControl CurrentPage
    {
        get => _currentPage;
        private set
        {
            if (ReferenceEquals(_currentPage, value))
            {
                return;
            }

            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    private string _currentPageKey;
    public string CurrentPageKey
    {
        get => _currentPageKey;
        private set
        {
            if (string.Equals(_currentPageKey, value, StringComparison.Ordinal))
            {
                return;
            }

            _currentPageKey = value;
            OnPropertyChanged(nameof(CurrentPageKey));
        }
    }

    public bool IsHomePage => string.Equals(CurrentPageKey, "Home", StringComparison.Ordinal);
    public bool IsProjectPage => string.Equals(CurrentPageKey, "Project", StringComparison.Ordinal);
    public bool IsSettingsPage => string.Equals(CurrentPageKey, "Settings", StringComparison.Ordinal);

    public void RegisterPage(string pageKey, Func<UserControl> pageFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pageKey);
        ArgumentNullException.ThrowIfNull(pageFactory);

        _pageFactories[pageKey] = pageFactory;
    }

    public void Navigate(string pageKey)
    {
        if (!_pageFactories.TryGetValue(pageKey, out var pageFactory))
        {
            throw new KeyNotFoundException($"Unknown navigation page: {pageKey}");
        }

        if (!_pages.TryGetValue(pageKey, out var page))
        {
            page = pageFactory.Invoke()
                ?? throw new InvalidOperationException($"Navigation page factory returned null: {pageKey}");
            _pages[pageKey] = page;
        }

        CurrentPageKey = pageKey;
        CurrentPage = page;
        OnPropertyChanged(nameof(IsHomePage));
        OnPropertyChanged(nameof(IsProjectPage));
        OnPropertyChanged(nameof(IsSettingsPage));
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
