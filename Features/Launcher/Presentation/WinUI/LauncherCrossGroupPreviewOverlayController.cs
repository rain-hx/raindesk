using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

internal readonly record struct LauncherCrossGroupPreviewOverlayEnsureResult(
    ListView View,
    ListView? ReplacedView,
    bool WasCreated);

/// <summary>
/// 持有跨分组预览 ListView 及其展示集合，统一创建、挂载、显示和隐藏生命周期。
/// </summary>
internal sealed class LauncherCrossGroupPreviewOverlayController
{
    private ListView? _view;
    private Action<ListViewItem, int, int>? _applyItemMargin;
    private Func<IconInfo, bool>? _isDataShortcut;
    private double _iconOpacity = 0.82d;

    public ListView? View => _view;

    public BulkObservableCollection<IconInfo> Tiles { get; } = new();

    public bool IsVisible => _view?.Visibility == Visibility.Visible;

    public LauncherCrossGroupPreviewOverlayEnsureResult Ensure(
        ListView source,
        Grid host,
        Action<ListViewItem, int, int> applyItemMargin,
        Func<IconInfo, bool> isDataShortcut,
        double iconOpacity = 0.82d)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(host);
        _applyItemMargin = applyItemMargin
            ?? throw new ArgumentNullException(nameof(applyItemMargin));
        _isDataShortcut = isDataShortcut
            ?? throw new ArgumentNullException(nameof(isDataShortcut));
        _iconOpacity = Math.Clamp(iconOpacity, 0d, 1d);

        ListView? replacedView = null;
        if (_view != null && !IsTransparentBrush(_view.Background))
        {
            if (host.Children.Contains(_view))
            {
                host.Children.Remove(_view);
            }

            _view.ContainerContentChanging -= HandleContainerContentChanging;
            replacedView = _view;
            _view = null;
        }

        if (_view != null)
        {
            return new LauncherCrossGroupPreviewOverlayEnsureResult(_view, replacedView, false);
        }

        _view = new ListView
        {
            ItemsSource = Tiles,
            ItemTemplate = source.ItemTemplate,
            ItemContainerStyle = source.ItemContainerStyle,
            ItemsPanel = source.ItemsPanel,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
            CanDragItems = false,
            CanReorderItems = false,
            AllowDrop = false,
            SelectionMode = ListViewSelectionMode.None,
            ShowsScrollingPlaceholders = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };
        ScrollViewer.SetHorizontalScrollBarVisibility(_view, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(_view, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollMode(_view, ScrollMode.Disabled);
        ScrollViewer.SetVerticalScrollMode(_view, ScrollMode.Enabled);

        int sourceIndex = host.Children.IndexOf(source);
        if (sourceIndex < 0)
        {
            host.Children.Add(_view);
        }
        else
        {
            host.Children.Insert(sourceIndex + 1, _view);
        }

        Canvas.SetZIndex(_view, 10);
        Grid.SetRow(_view, Grid.GetRow(source));
        Grid.SetColumn(_view, Grid.GetColumn(source));
        Grid.SetRowSpan(_view, Grid.GetRowSpan(source));
        Grid.SetColumnSpan(_view, Grid.GetColumnSpan(source));
        _view.ContainerContentChanging += HandleContainerContentChanging;
        return new LauncherCrossGroupPreviewOverlayEnsureResult(_view, replacedView, true);
    }

    public bool ShowPreview(
        ListView source,
        Grid host,
        IReadOnlyList<IconInfo> tiles,
        Action<ListViewItem, int, int> applyItemMargin,
        Func<IconInfo, bool> isDataShortcut,
        Action<ListView> applyLayout,
        Action applyItemChrome,
        DispatcherQueue dispatcherQueue,
        int token,
        Func<int, bool> isCurrent,
        Action<int> layoutCompleted,
        double iconOpacity = 0.82d)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(applyLayout);
        ArgumentNullException.ThrowIfNull(applyItemChrome);
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        ArgumentNullException.ThrowIfNull(isCurrent);
        ArgumentNullException.ThrowIfNull(layoutCompleted);

        LauncherCrossGroupPreviewOverlayEnsureResult ensured = Ensure(
            source,
            host,
            applyItemMargin,
            isDataShortcut,
            iconOpacity);
        ListView overlay = ensured.View;
        PrepareBitmaps(tiles, isDataShortcut);
        Show(tiles);
        ApplyTransparency(source, host, hideSource: true);
        applyLayout(overlay);
        applyItemChrome();
        overlay.UpdateLayout();
        source.IsHitTestVisible = false;

        dispatcherQueue.TryEnqueue(() =>
        {
            if (!IsVisible || !isCurrent(token))
            {
                return;
            }

            applyLayout(overlay);
            ApplyTransparency(source, host, hideSource: true);
            applyItemChrome();
            overlay.UpdateLayout();
            layoutCompleted(token);
        });
        return true;
    }

    public void Show(IReadOnlyList<IconInfo> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        Tiles.ReplaceAll(tiles);
        if (_view != null)
        {
            _view.Visibility = Visibility.Visible;
        }
    }

    public void Hide()
    {
        if (_view != null)
        {
            _view.Visibility = Visibility.Collapsed;
        }
    }

    public void HidePreview(ListView? source, Grid? host)
    {
        Hide();
        if (source != null)
        {
            source.IsHitTestVisible = true;
            source.Opacity = 1d;
        }

        if (host != null)
        {
            host.Background = null;
        }
    }

    private void HandleContainerContentChanging(
        ListViewBase sender,
        ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue
            || args.Item is not IconInfo icon
            || args.ItemContainer is not ListViewItem item)
        {
            return;
        }

        if (args.Phase == 0)
        {
            item.Opacity = 1d;
            item.Visibility = Visibility.Visible;
            item.IsHitTestVisible = false;
            if (args.ItemIndex >= 0)
            {
                _applyItemMargin?.Invoke(item, args.ItemIndex, Tiles.Count);
            }

            args.RegisterUpdateCallback(HandleContainerContentChanging);
            args.Handled = true;
            return;
        }

        if (args.Phase == 1)
        {
            if (_isDataShortcut?.Invoke(icon) == true)
            {
                ImageUtil.EnsureIconBitmap(icon);
                item.Opacity = _iconOpacity;
                if (item.ContentTemplateRoot is UIElement root)
                {
                    root.Opacity = _iconOpacity;
                }
            }

            args.Handled = true;
        }
    }

    private static void PrepareBitmaps(
        IReadOnlyList<IconInfo> tiles,
        Func<IconInfo, bool> isDataShortcut)
    {
        foreach (IconInfo icon in tiles)
        {
            if (icon != null && isDataShortcut(icon))
            {
                ImageUtil.EnsureIconBitmap(icon);
            }
        }
    }

    private void ApplyTransparency(ListView source, Grid host, bool hideSource)
    {
        var transparent = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        if (_view != null)
        {
            _view.Background = transparent;
            if (FindDescendantScrollViewer(_view) is ScrollViewer overlayScrollViewer)
            {
                overlayScrollViewer.Background = transparent;
            }
        }

        source.Opacity = hideSource ? 0d : 1d;
        source.Background = transparent;
        if (FindDescendantScrollViewer(source) is ScrollViewer sourceScrollViewer)
        {
            sourceScrollViewer.Background = transparent;
        }

        host.Background = transparent;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int index = 0; index < count; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            ScrollViewer? descendant = FindDescendantScrollViewer(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static bool IsTransparentBrush(Brush? brush) =>
        brush is SolidColorBrush solid && solid.Color.A == 0;
}
