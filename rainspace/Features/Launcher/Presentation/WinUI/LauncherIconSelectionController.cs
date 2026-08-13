using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace rainspace.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Coordinates ListView selection, modifier keys, marquee geometry and model selection visuals.
/// Concrete shortcut properties remain behind injected accessors.
/// </summary>
internal sealed class LauncherIconSelectionController<TItem>
    where TItem : class
{
    private readonly LauncherIconSelectionState<TItem> _state = new();
    private readonly Func<ListView?> _getListView;
    private readonly Func<FrameworkElement?> _getHost;
    private readonly Func<Canvas?> _getMarqueeCanvas;
    private readonly Func<Rectangle?> _getMarqueeRectangle;
    private readonly Func<IReadOnlyList<TItem>> _getVisibleItems;
    private readonly Func<IEnumerable<TItem>> _getAllItems;
    private readonly Func<TItem, bool> _isExcluded;
    private readonly Func<bool> _isBatchMode;
    private readonly Func<bool> _isCustomDragActive;
    private readonly Func<double> _getCardWidth;
    private readonly Func<TItem, bool> _getListSelectedVisual;
    private readonly Action<TItem, bool, bool> _setSelectionVisual;

    public LauncherIconSelectionController(
        Func<ListView?> getListView,
        Func<FrameworkElement?> getHost,
        Func<Canvas?> getMarqueeCanvas,
        Func<Rectangle?> getMarqueeRectangle,
        Func<IReadOnlyList<TItem>> getVisibleItems,
        Func<IEnumerable<TItem>> getAllItems,
        Func<TItem, bool> isExcluded,
        Func<bool> isBatchMode,
        Func<bool> isCustomDragActive,
        Func<double> getCardWidth,
        Func<TItem, bool> getListSelectedVisual,
        Action<TItem, bool, bool> setSelectionVisual)
    {
        _getListView = getListView ?? throw new ArgumentNullException(nameof(getListView));
        _getHost = getHost ?? throw new ArgumentNullException(nameof(getHost));
        _getMarqueeCanvas = getMarqueeCanvas
            ?? throw new ArgumentNullException(nameof(getMarqueeCanvas));
        _getMarqueeRectangle = getMarqueeRectangle
            ?? throw new ArgumentNullException(nameof(getMarqueeRectangle));
        _getVisibleItems = getVisibleItems ?? throw new ArgumentNullException(nameof(getVisibleItems));
        _getAllItems = getAllItems ?? throw new ArgumentNullException(nameof(getAllItems));
        _isExcluded = isExcluded ?? throw new ArgumentNullException(nameof(isExcluded));
        _isBatchMode = isBatchMode ?? throw new ArgumentNullException(nameof(isBatchMode));
        _isCustomDragActive = isCustomDragActive
            ?? throw new ArgumentNullException(nameof(isCustomDragActive));
        _getCardWidth = getCardWidth ?? throw new ArgumentNullException(nameof(getCardWidth));
        _getListSelectedVisual = getListSelectedVisual
            ?? throw new ArgumentNullException(nameof(getListSelectedVisual));
        _setSelectionVisual = setSelectionVisual
            ?? throw new ArgumentNullException(nameof(setSelectionVisual));
    }

    public bool IsMarqueeActive => _state.IsMarqueeActive;

    public static bool IsControlKeyDown() =>
        IsKeyDown(VirtualKey.LeftControl) || IsKeyDown(VirtualKey.RightControl);

    public static bool IsShiftKeyDown() =>
        IsKeyDown(VirtualKey.LeftShift) || IsKeyDown(VirtualKey.RightShift);

    public static bool IsMultiSelectModifierDown() =>
        IsControlKeyDown() || IsShiftKeyDown();

    public void HandleSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        SyncVisuals();
    }

    public void SyncVisuals()
    {
        ListView? listView = _getListView();
        if (listView == null)
        {
            return;
        }

        var selected = new HashSet<TItem>(listView.SelectedItems.OfType<TItem>());
        bool updateChecked = _isBatchMode();
        foreach (TItem item in listView.Items.OfType<TItem>())
        {
            if (!_isExcluded(item))
            {
                _setSelectionVisual(item, selected.Contains(item), updateChecked);
            }
        }
    }

    public void ClearSelection()
    {
        _state.ResetAnchor();
        _getListView()?.SelectedItems.Clear();
        bool updateChecked = _isBatchMode();
        foreach (TItem item in _getAllItems())
        {
            _setSelectionVisual(item, false, updateChecked);
        }
    }

    public void HandleItemPointerPressed(
        TItem item,
        PointerRoutedEventArgs args,
        bool interactionEnabled)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(args);
        ListView? listView = _getListView();
        if (!interactionEnabled
            || listView == null
            || _isExcluded(item)
            || !args.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        {
            return;
        }

        bool control = IsControlKeyDown();
        bool shift = IsShiftKeyDown();
        if (control && !shift)
        {
            Toggle(item, listView);
            args.Handled = true;
            return;
        }

        if (shift)
        {
            SelectRangeTo(item, additive: control, listView);
            args.Handled = true;
            return;
        }

        _state.SetAnchor(IndexOf(_getVisibleItems(), item));
        if (listView.SelectedItems.Count > 0 && !listView.SelectedItems.Contains(item))
        {
            listView.SelectedItems.Clear();
            SyncVisuals();
        }
    }

    public void EnsurePrimaryDragSelection(TItem primaryItem, int draggedItemCount)
    {
        ArgumentNullException.ThrowIfNull(primaryItem);
        ListView? listView = _getListView();
        if (listView == null)
        {
            return;
        }

        if (draggedItemCount > 1
            || listView.SelectedItems.Count != 1
            || !listView.SelectedItems.Contains(primaryItem))
        {
            listView.SelectedItems.Clear();
            listView.SelectedItems.Add(primaryItem);
            SyncVisuals();
        }
    }

    public void ReplaceSelectionObjects(IEnumerable<object> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        ListView? listView = _getListView();
        if (listView == null)
        {
            return;
        }

        listView.SelectedItems.Clear();
        foreach (TItem item in items.OfType<TItem>().Where(item => !_isExcluded(item)))
        {
            listView.SelectedItems.Add(item);
        }
        SyncVisuals();
    }

    public void PrepareDragVisuals(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        TItem[] snapshotItems = items.Distinct().ToArray();
        _state.CaptureDragVisuals(snapshotItems.Select(item =>
            new KeyValuePair<TItem, bool>(item, _getListSelectedVisual(item))));
        foreach (TItem item in snapshotItems)
        {
            _setSelectionVisual(item, false, false);
        }
    }

    public void RestoreDragVisuals()
    {
        foreach (KeyValuePair<TItem, bool> snapshot in _state.TakeDragVisuals())
        {
            _setSelectionVisual(snapshot.Key, snapshot.Value, false);
        }
    }

    public void HandleHostPointerPressed(PointerRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        FrameworkElement? host = _getHost();
        ListView? listView = _getListView();
        if (host == null
            || listView == null
            || _isCustomDragActive()
            || _state.IsMarqueeActive
            || !args.GetCurrentPoint(host).Properties.IsLeftButtonPressed)
        {
            return;
        }

        Point point = args.GetCurrentPoint(host).Position;
        if (TryGetItemAtPoint(point, out _))
        {
            return;
        }

        bool additive = IsMultiSelectModifierDown();
        _state.BeginMarquee(
            args.Pointer.PointerId,
            new LauncherSelectionPoint(point.X, point.Y),
            additive,
            listView.SelectedItems.OfType<TItem>());
        host.CapturePointer(args.Pointer);
        if (!additive)
        {
            listView.SelectedItems.Clear();
            SyncVisuals();
        }

        UpdateMarqueeVisual(point, point);
        ApplyMarqueeSelection(point, point, args.Pointer.PointerId);
        args.Handled = true;
    }

    public void HandleHostPointerMoved(PointerRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        FrameworkElement? host = _getHost();
        if (host == null
            || !_state.TryGetMarquee(args.Pointer.PointerId, out LauncherMarqueeSnapshot<TItem> session))
        {
            return;
        }

        Point point = args.GetCurrentPoint(host).Position;
        Point start = new(session.Start.X, session.Start.Y);
        UpdateMarqueeVisual(start, point);
        ApplyMarqueeSelection(start, point, args.Pointer.PointerId);
        args.Handled = true;
    }

    public void HandleHostPointerReleased(PointerRoutedEventArgs args) =>
        CompleteMarquee(args);

    public void HandleHostPointerCanceled(PointerRoutedEventArgs args) =>
        CompleteMarquee(args);

    private void Toggle(TItem item, ListView listView)
    {
        if (listView.SelectedItems.Contains(item))
        {
            listView.SelectedItems.Remove(item);
        }
        else
        {
            listView.SelectedItems.Add(item);
        }

        _state.SetAnchor(IndexOf(_getVisibleItems(), item));
        SyncVisuals();
    }

    private void SelectRangeTo(TItem item, bool additive, ListView listView)
    {
        IReadOnlyList<TItem> items = _getVisibleItems();
        int endIndex = IndexOf(items, item);
        LauncherSelectionRange? range = _state.ResolveRangeTo(endIndex, items.Count);
        if (range == null)
        {
            return;
        }

        if (!additive)
        {
            listView.SelectedItems.Clear();
        }

        for (int i = range.Value.StartIndex; i <= range.Value.EndIndex; i++)
        {
            TItem candidate = items[i];
            if (!_isExcluded(candidate) && !listView.SelectedItems.Contains(candidate))
            {
                listView.SelectedItems.Add(candidate);
            }
        }
        SyncVisuals();
    }

    private bool TryGetItemAtPoint(Point pointInHost, out TItem? item)
    {
        item = null;
        ListView? listView = _getListView();
        FrameworkElement? host = _getHost();
        if (listView == null || host == null)
        {
            return false;
        }

        listView.UpdateLayout();
        IReadOnlyList<TItem> items = _getVisibleItems();
        for (int i = 0; i < items.Count; i++)
        {
            TItem candidate = items[i];
            if (_isExcluded(candidate)
                || listView.ContainerFromIndex(i) is not ListViewItem container)
            {
                continue;
            }

            if (GetItemHitRect(container, host, _getCardWidth()).Contains(pointInHost))
            {
                item = candidate;
                return true;
            }
        }
        return false;
    }

    private void CompleteMarquee(PointerRoutedEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!_state.CompleteMarquee(args.Pointer.PointerId))
        {
            return;
        }

        _getHost()?.ReleasePointerCaptures();
        HideMarqueeVisual();
        SyncVisuals();
        args.Handled = true;
    }

    private void ApplyMarqueeSelection(Point start, Point end, uint pointerId)
    {
        ListView? listView = _getListView();
        FrameworkElement? host = _getHost();
        if (listView == null
            || host == null
            || !_state.TryGetMarquee(pointerId, out LauncherMarqueeSnapshot<TItem> session))
        {
            return;
        }

        Rect marquee = NormalizeRect(start, end);
        var selected = new List<TItem>(session.BaselineSelection);
        listView.UpdateLayout();
        IReadOnlyList<TItem> items = _getVisibleItems();
        for (int i = 0; i < items.Count; i++)
        {
            TItem item = items[i];
            if (_isExcluded(item)
                || listView.ContainerFromIndex(i) is not ListViewItem container
                || !Intersects(marquee, GetItemHitRect(container, host, _getCardWidth())))
            {
                continue;
            }

            if (!selected.Contains(item))
            {
                selected.Add(item);
            }
        }

        listView.SelectedItems.Clear();
        foreach (TItem item in selected)
        {
            listView.SelectedItems.Add(item);
        }
        if (selected.Count > 0)
        {
            _state.SetAnchor(IndexOf(items, selected[^1]));
        }
    }

    private void UpdateMarqueeVisual(Point start, Point end)
    {
        Canvas? canvas = _getMarqueeCanvas();
        Rectangle? rectangle = _getMarqueeRectangle();
        if (canvas == null || rectangle == null)
        {
            return;
        }

        Rect bounds = NormalizeRect(start, end);
        canvas.Visibility = Visibility.Visible;
        rectangle.Visibility = Visibility.Visible;
        Canvas.SetLeft(rectangle, bounds.X);
        Canvas.SetTop(rectangle, bounds.Y);
        rectangle.Width = Math.Max(1d, bounds.Width);
        rectangle.Height = Math.Max(1d, bounds.Height);
    }

    private void HideMarqueeVisual()
    {
        if (_getMarqueeCanvas() is { } canvas)
        {
            canvas.Visibility = Visibility.Collapsed;
        }
        if (_getMarqueeRectangle() is { } rectangle)
        {
            rectangle.Visibility = Visibility.Collapsed;
        }
    }

    private static Rect GetItemHitRect(
        ListViewItem item,
        UIElement relativeTo,
        double cardWidth)
    {
        Rect bounds = item.TransformToVisual(relativeTo)
            .TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
        double width = Math.Min(cardWidth, bounds.Width);
        double left = bounds.X + (bounds.Width - width) / 2d;
        return new Rect(left, bounds.Y, width, bounds.Height);
    }

    private static Rect NormalizeRect(Point start, Point end) => new(
        Math.Min(start.X, end.X),
        Math.Min(start.Y, end.Y),
        Math.Abs(end.X - start.X),
        Math.Abs(end.Y - start.Y));

    private static bool Intersects(Rect first, Rect second) =>
        first.X < second.X + second.Width
        && first.X + first.Width > second.X
        && first.Y < second.Y + second.Height
        && first.Y + first.Height > second.Y;

    private static int IndexOf(IReadOnlyList<TItem> items, TItem target)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (EqualityComparer<TItem>.Default.Equals(items[i], target))
            {
                return i;
            }
        }
        return -1;
    }

    private static bool IsKeyDown(VirtualKey key) =>
        InputKeyboardSource.GetKeyStateForCurrentThread(key)
            .HasFlag(CoreVirtualKeyStates.Down);
}
