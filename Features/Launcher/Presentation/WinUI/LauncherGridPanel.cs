using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.Launcher.Application;
using raindesk.ViewModel;
using System.ComponentModel;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

public sealed class LauncherGridPanel : Panel
{
    private const double DefaultItemWidth = 88d;
    private const double DefaultItemHeight = 94d;
    private readonly Dictionary<UIElement, INotifyPropertyChanged> trackedChildNotifications = new();

    public static readonly DependencyProperty ColumnCountProperty =
        DependencyProperty.Register(
            nameof(ColumnCount),
            typeof(int),
            typeof(LauncherGridPanel),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(LauncherGridPanel),
            new PropertyMetadata(DefaultItemWidth, OnLayoutPropertyChanged));

    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(LauncherGridPanel),
            new PropertyMetadata(DefaultItemHeight, OnLayoutPropertyChanged));

    public static readonly DependencyProperty UseManualPositionsProperty =
        DependencyProperty.Register(
            nameof(UseManualPositions),
            typeof(bool),
            typeof(LauncherGridPanel),
            new PropertyMetadata(true, OnLayoutPropertyChanged));

    public LauncherGridPanel()
    {
        Unloaded += LauncherGridPanel_Unloaded;
    }

    public int ColumnCount
    {
        get => (int)GetValue(ColumnCountProperty);
        set => SetValue(ColumnCountProperty, value);
    }

    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    public bool UseManualPositions
    {
        get => (bool)GetValue(UseManualPositionsProperty);
        set => SetValue(UseManualPositionsProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = BuildLayout(availableSize, out LauncherGridMetrics metrics);
        foreach (UIElement child in Children)
        {
            if (!CanMeasureChild(child))
            {
                continue;
            }

            Size childSize = layout.TryGetValue(child, out LauncherGridRect rect)
                ? new Size(rect.Bounds.Width, rect.Bounds.Height)
                : new Size(metrics.SlotWidth, metrics.SlotHeight);
            childSize = CoerceMeasureSize(child, childSize, metrics);
            child.Measure(childSize);
        }

        LauncherGridExtent extent = LauncherGridLayoutEngine.CalculateExtent(layout, metrics);
        double width = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0d
            ? extent.Width
            : availableSize.Width;
        return new Size(width, extent.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var layout = BuildLayout(finalSize, out LauncherGridMetrics metrics);
        foreach (UIElement child in Children)
        {
            if (child == null)
            {
                continue;
            }

            if (layout.TryGetValue(child, out LauncherGridRect rect))
            {
                child.Arrange(new Rect(
                    rect.Bounds.X,
                    rect.Bounds.Y,
                    rect.Bounds.Width,
                    rect.Bounds.Height));
            }
            else
            {
                child.Arrange(new Rect(0d, 0d, 0d, 0d));
            }
        }

        LauncherGridExtent extent = LauncherGridLayoutEngine.CalculateExtent(layout, metrics);
        double width = double.IsInfinity(finalSize.Width) || finalSize.Width <= 0d
            ? extent.Width
            : finalSize.Width;
        return new Size(width, extent.Height);
    }

    private Dictionary<object, LauncherGridRect> BuildLayout(
        Size availableSize,
        out LauncherGridMetrics metrics)
    {
        int columnCount = ResolveColumnCount(availableSize);
        double itemWidth = ResolveItemWidth(availableSize, columnCount);
        double itemHeight = ResolveItemHeight();
        metrics = new LauncherGridMetrics(columnCount, itemWidth, itemHeight);

        var items = new List<LauncherGridItem>(Children.Count);
        var currentChildren = new HashSet<UIElement>();
        foreach (UIElement child in Children)
        {
            if (!CanLayoutChild(child))
            {
                continue;
            }

            currentChildren.Add(child);
            IconInfo? icon = ResolveIcon(child);
            TrackChildIcon(child, icon);
            int columnSpan = GetShortcutColumnSpan(icon);
            int rowSpan = GetShortcutRowSpan(icon);
            items.Add(UseManualPositions && icon?.HasManualLauncherGridPosition_NoWrite == true
                ? new LauncherGridItem(
                    child,
                    columnSpan,
                    rowSpan,
                    icon.LauncherGridColumn,
                    icon.LauncherGridRow)
                : new LauncherGridItem(child, columnSpan, rowSpan));
        }

        UntrackRemovedChildren(currentChildren);
        return LauncherGridLayoutEngine.Arrange(items, metrics);
    }

    private static bool CanMeasureChild(UIElement? child)
    {
        if (!CanLayoutChild(child))
        {
            return false;
        }

        return true;
    }

    private static bool CanLayoutChild(UIElement? child)
    {
        if (child == null || child.Visibility == Visibility.Collapsed)
        {
            return false;
        }

        return child is not ListViewItem { Content: null };
    }

    private static Size CoerceMeasureSize(UIElement child, Size size, LauncherGridMetrics metrics)
    {
        if (child is ListViewItem)
        {
            return new Size(Math.Max(1d, metrics.SlotWidth), Math.Max(1d, metrics.SlotHeight));
        }

        double width = IsUsableLength(size.Width) ? size.Width : metrics.SlotWidth;
        double height = IsUsableLength(size.Height) ? size.Height : metrics.SlotHeight;
        return new Size(Math.Max(1d, width), Math.Max(1d, height));
    }

    private static bool IsUsableLength(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0d;
    }

    private int ResolveColumnCount(Size availableSize)
    {
        if (ColumnCount > 0)
        {
            return ColumnCount;
        }

        double width = double.IsInfinity(availableSize.Width) || availableSize.Width <= 0d
            ? ActualWidth
            : availableSize.Width;
        double itemWidth = ItemWidth > 0d ? ItemWidth : DefaultItemWidth;

        if (width <= 0d)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Floor(width / itemWidth));
    }

    private double ResolveItemWidth(Size availableSize, int columnCount)
    {
        if (ItemWidth > 0d)
        {
            return ItemWidth;
        }

        if (!double.IsInfinity(availableSize.Width) && availableSize.Width > 0d)
        {
            return Math.Max(1d, availableSize.Width / Math.Max(1, columnCount));
        }

        return DefaultItemWidth;
    }

    private double ResolveItemHeight()
    {
        return ItemHeight > 0d ? ItemHeight : DefaultItemHeight;
    }

    private static IconInfo? ResolveIcon(UIElement child)
    {
        if (child is ContentControl contentControl && contentControl.Content is IconInfo contentIcon)
        {
            return contentIcon;
        }

        return (child as FrameworkElement)?.DataContext as IconInfo;
    }

    private static int GetShortcutColumnSpan(IconInfo? icon)
    {
        if (icon?.IsDragYieldPlaceholder_NoWrite == true)
        {
            return Math.Max(1, icon.DragPlaceholderColumnSpan_NoWrite);
        }

        return icon?.IsFolder == true ? Math.Max(1, icon.FolderColumnSpan_NoWrite) : 1;
    }

    private static int GetShortcutRowSpan(IconInfo? icon)
    {
        if (icon?.IsDragYieldPlaceholder_NoWrite == true)
        {
            return Math.Max(1, icon.DragPlaceholderRowSpan_NoWrite);
        }

        return icon?.IsFolder == true ? Math.Max(1, icon.FolderRowSpan_NoWrite) : 1;
    }

    private void TrackChildIcon(UIElement child, IconInfo? icon)
    {
        if (icon == null)
        {
            UntrackChild(child);
            return;
        }

        INotifyPropertyChanged notifyingIcon = icon;
        if (trackedChildNotifications.TryGetValue(child, out INotifyPropertyChanged? trackedIcon)
            && ReferenceEquals(trackedIcon, notifyingIcon))
        {
            return;
        }

        UntrackChild(child);
        trackedChildNotifications[child] = notifyingIcon;
        notifyingIcon.PropertyChanged += Icon_PropertyChanged;
    }

    private void UntrackRemovedChildren(IReadOnlySet<UIElement> currentChildren)
    {
        foreach (UIElement child in trackedChildNotifications.Keys.ToList())
        {
            if (!currentChildren.Contains(child))
            {
                UntrackChild(child);
            }
        }
    }

    private void UntrackChild(UIElement child)
    {
        if (trackedChildNotifications.Remove(child, out INotifyPropertyChanged? icon))
        {
            icon.PropertyChanged -= Icon_PropertyChanged;
        }
    }

    private void UntrackAllChildren()
    {
        foreach (INotifyPropertyChanged icon in trackedChildNotifications.Values)
        {
            icon.PropertyChanged -= Icon_PropertyChanged;
        }

        trackedChildNotifications.Clear();
    }

    private void Icon_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(IconInfo.FolderSize)
            || e.PropertyName == nameof(IconInfo.FolderColumnSpan_NoWrite)
            || e.PropertyName == nameof(IconInfo.FolderRowSpan_NoWrite)
            || e.PropertyName == nameof(IconInfo.DragPlaceholderColumnSpan_NoWrite)
            || e.PropertyName == nameof(IconInfo.DragPlaceholderRowSpan_NoWrite)
            || e.PropertyName == nameof(IconInfo.LauncherGridColumn)
            || e.PropertyName == nameof(IconInfo.LauncherGridRow)
            || e.PropertyName == nameof(IconInfo.IsFolder))
        {
            InvalidateMeasure();
            InvalidateArrange();
        }
    }

    private void LauncherGridPanel_Unloaded(object sender, RoutedEventArgs e)
    {
        UntrackAllChildren();
    }

    private static void OnLayoutPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is LauncherGridPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }
}
