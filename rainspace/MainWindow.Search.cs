using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using rainspace.Controls;
using rainspace.Control.Other;
using rainspace.Features.Search.Contracts;
using rainspace.Features.Search.Presentation;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;
using Windows.Foundation;

namespace rainspace;

public sealed partial class MainWindow
{
    private readonly BulkObservableCollection<IconInfo> _searchResults = new();
    private SearchResControl? _searchResultsControl;
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _searchDebounceTimer;
    private Storyboard? _searchResultsDropDownStoryboard;
    private CancellationTokenSource? _searchCancellation;
    private const int SearchDebounceMs = 200;
    private string? _pendingSearchKeyword;

    private void CloseSearchResultsDropDown()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
        _searchResultsDropDownStoryboard?.Stop();
        if (SearchResultsDropDown != null)
        {
            SearchResultsDropDown.Opacity = 0;
            if (SearchResultsDropDown.RenderTransform is TranslateTransform transform)
            {
                transform.Y = -6;
            }

            SearchResultsDropDown.Visibility = Visibility.Collapsed;
        }

        _searchResults.Clear();
        _pendingSearchKeyword = null;
    }

    private void EnsureSearchResultsHost()
    {
        if (_searchResultsControl != null)
        {
            return;
        }

        _searchResultsControl = new SearchResControl(_searchResults)
        {
            MaxHeight = 252,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        SearchResultsHost.Content = _searchResultsControl;
    }

    private void UpdateSearchResultsDropDown()
    {
        if (SearchResultsDropDown == null || SearchBoxHost == null)
        {
            return;
        }

        if (_searchResults.Count == 0)
        {
            CloseSearchResultsDropDown();
            return;
        }

        bool wasCollapsed = SearchResultsDropDown.Visibility != Visibility.Visible;
        Canvas.SetZIndex(SearchResultsDropDown, 9000);
        SearchResultsDropDown.Visibility = Visibility.Visible;
        UpdateSearchResultsDropDownPosition();
        if (wasCollapsed)
        {
            AnimateSearchResultsDropDownIn();
        }

        DispatcherQueue.TryEnqueue(UpdateSearchResultsDropDownPosition);
    }

    private void AnimateSearchResultsDropDownIn()
    {
        _searchResultsDropDownStoryboard?.Stop();
        if (SearchResultsDropDown == null)
        {
            return;
        }

        if (SearchResultsDropDown.RenderTransform is not TranslateTransform transform)
        {
            transform = new TranslateTransform();
            SearchResultsDropDown.RenderTransform = transform;
        }

        if (!MotionPreferences.ShouldAnimate)
        {
            SearchResultsDropDown.Opacity = 1;
            transform.Y = 0;
            return;
        }

        SearchResultsDropDown.Opacity = 0;
        transform.Y = -6;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var fade = new DoubleAnimation
        {
            To = 1,
            Duration = TimeSpan.FromMilliseconds(140),
            EasingFunction = ease
        };
        var slide = new DoubleAnimation
        {
            To = 0,
            Duration = TimeSpan.FromMilliseconds(180),
            EasingFunction = ease
        };

        Storyboard.SetTarget(fade, SearchResultsDropDown);
        Storyboard.SetTargetProperty(fade, nameof(UIElement.Opacity));
        Storyboard.SetTarget(slide, transform);
        Storyboard.SetTargetProperty(slide, nameof(TranslateTransform.Y));

        _searchResultsDropDownStoryboard = new Storyboard();
        _searchResultsDropDownStoryboard.Children.Add(fade);
        _searchResultsDropDownStoryboard.Children.Add(slide);
        _searchResultsDropDownStoryboard.Begin();
    }

    private void UpdateSearchResultsDropDownPosition()
    {
        if (SearchResultsDropDown == null || SearchBoxHost == null)
        {
            return;
        }

        if (SearchResultsDropDown.Parent is not FrameworkElement hostRoot)
        {
            return;
        }

        double dropDownWidth = SearchBoxHost.ActualWidth > 0d
            ? SearchBoxHost.ActualWidth
            : SearchBoxHost.Width;
        double dropDownTop = SearchBoxHost.ActualHeight > 0d
            ? SearchBoxHost.ActualHeight
            : SearchBoxHost.Height;

        if (dropDownWidth <= 0d)
        {
            dropDownWidth = 420d;
        }

        if (dropDownTop <= 0d)
        {
            dropDownTop = 44d;
        }

        GeneralTransform transform = SearchBoxHost.TransformToVisual(hostRoot);
        Rect bounds = transform.TransformBounds(new Rect(0, 0, dropDownWidth, dropDownTop));

        const double gap = 6d;
        SearchResultsDropDown.Width = dropDownWidth;
        SearchResultsDropDown.MaxWidth = dropDownWidth;
        SearchResultsDropDown.MinWidth = dropDownWidth;
        SearchResultsDropDown.Margin = new Thickness(bounds.X, bounds.Y + dropDownTop + gap, 0, 0);
        SearchResultsDropDown.HorizontalAlignment = HorizontalAlignment.Left;
        SearchResultsDropDown.VerticalAlignment = VerticalAlignment.Top;
    }

    private void OnSearchBoxTextChanged(string? keyword)
    {
        if (_searchDebounceTimer == null)
        {
            _searchDebounceTimer = DispatcherQueue.CreateTimer();
            _searchDebounceTimer.Tick += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                _ = ExecuteSearchAsync(_pendingSearchKeyword);
            };
        }

        _pendingSearchKeyword = keyword;
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(SearchDebounceMs);
        _searchDebounceTimer.Start();
    }

    private async global::System.Threading.Tasks.Task ExecuteSearchAsync(string? keyword)
    {
        EnsureSearchResultsHost();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            _searchResults.ReplaceAll(Array.Empty<IconInfo>());
            UpdateSearchResultsDropDown();
            return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        var cancellation = new CancellationTokenSource();
        _searchCancellation = cancellation;
        _pendingSearchKeyword = keyword;

        var dataProvider = AppServices.TryGet<IAppDataProvider>();
        var queryHandler = AppServices.TryGet<ISearchQueryHandler>();
        if (dataProvider == null || queryHandler == null)
        {
            _searchResults.ReplaceAll(Array.Empty<IconInfo>());
            UpdateSearchResultsDropDown();
            return;
        }

        bool includeEverything = dataProvider.Current.AppConfig.EnableEveryThing == true;
        IReadOnlyList<SearchResult> results;
        try
        {
            results = await queryHandler.HandleAsync(
                new SearchQuery(keyword, includeEverything),
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (keyword != _pendingSearchKeyword
            || !ReferenceEquals(_searchCancellation, cancellation))
        {
            return;
        }

        _searchResults.ReplaceAll(results.Select(SearchResultPresentationMapper.ToIconInfo));

        UpdateSearchResultsDropDown();
    }
}
