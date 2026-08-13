using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Owns WinUI group-bar drag-over hit testing, edge scrolling, hover indicators and preview switching.
/// </summary>
internal sealed class LauncherGroupBarDragController<TMenu, TItem>
    where TMenu : class
    where TItem : class
{
    private readonly LauncherGroupTabHoverState<TMenu> _hover = new();
    private readonly Func<ListView?> _getListView;
    private readonly Func<FrameworkElement?> _getGroupBar;
    private readonly Func<ScrollViewer?> _getScrollViewer;
    private readonly Func<IReadOnlyList<TMenu>> _getMenus;
    private readonly Action<TMenu, bool> _setIndicator;
    private readonly Func<bool> _isCustomDragActive;
    private readonly Func<bool> _isPreviewActive;
    private readonly Func<TMenu?> _getPreviewTarget;
    private readonly Func<DragEventArgs, bool> _isPointerOverShortcutArea;
    private readonly Func<IReadOnlyList<TItem>> _getDraggedItems;
    private readonly Func<TItem?> _getPrimaryDraggedItem;
    private readonly Func<IReadOnlyList<TItem>, TMenu, bool> _canPreview;
    private readonly Action<TMenu> _applyPreview;
    private readonly Action _restorePreview;
    private readonly Func<BitmapImage> _getEmptyDragBitmap;

    public LauncherGroupBarDragController(
        Func<ListView?> getListView,
        Func<FrameworkElement?> getGroupBar,
        Func<ScrollViewer?> getScrollViewer,
        Func<IReadOnlyList<TMenu>> getMenus,
        Action<TMenu, bool> setIndicator,
        Func<bool> isCustomDragActive,
        Func<bool> isPreviewActive,
        Func<TMenu?> getPreviewTarget,
        Func<DragEventArgs, bool> isPointerOverShortcutArea,
        Func<IReadOnlyList<TItem>> getDraggedItems,
        Func<TItem?> getPrimaryDraggedItem,
        Func<IReadOnlyList<TItem>, TMenu, bool> canPreview,
        Action<TMenu> applyPreview,
        Action restorePreview,
        Func<BitmapImage> getEmptyDragBitmap)
    {
        _getListView = getListView ?? throw new ArgumentNullException(nameof(getListView));
        _getGroupBar = getGroupBar ?? throw new ArgumentNullException(nameof(getGroupBar));
        _getScrollViewer = getScrollViewer ?? throw new ArgumentNullException(nameof(getScrollViewer));
        _getMenus = getMenus ?? throw new ArgumentNullException(nameof(getMenus));
        _setIndicator = setIndicator ?? throw new ArgumentNullException(nameof(setIndicator));
        _isCustomDragActive = isCustomDragActive ?? throw new ArgumentNullException(nameof(isCustomDragActive));
        _isPreviewActive = isPreviewActive ?? throw new ArgumentNullException(nameof(isPreviewActive));
        _getPreviewTarget = getPreviewTarget ?? throw new ArgumentNullException(nameof(getPreviewTarget));
        _isPointerOverShortcutArea = isPointerOverShortcutArea
            ?? throw new ArgumentNullException(nameof(isPointerOverShortcutArea));
        _getDraggedItems = getDraggedItems ?? throw new ArgumentNullException(nameof(getDraggedItems));
        _getPrimaryDraggedItem = getPrimaryDraggedItem
            ?? throw new ArgumentNullException(nameof(getPrimaryDraggedItem));
        _canPreview = canPreview ?? throw new ArgumentNullException(nameof(canPreview));
        _applyPreview = applyPreview ?? throw new ArgumentNullException(nameof(applyPreview));
        _restorePreview = restorePreview ?? throw new ArgumentNullException(nameof(restorePreview));
        _getEmptyDragBitmap = getEmptyDragBitmap
            ?? throw new ArgumentNullException(nameof(getEmptyDragBitmap));
    }

    public TMenu? HoverTarget => _hover.Target;

    public bool HandleDragOver(DragEventArgs args, string itemPropertyKey)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemPropertyKey);
        if (!args.DataView.Properties.ContainsKey(itemPropertyKey))
        {
            args.AcceptedOperation = DataPackageOperation.None;
            return false;
        }

        args.AcceptedOperation = DataPackageOperation.Link;
        args.DragUIOverride.Caption = string.Empty;
        args.DragUIOverride.IsContentVisible = false;
        args.DragUIOverride.IsCaptionVisible = false;
        args.DragUIOverride.IsGlyphVisible = false;
        if (_getDraggedItems().Count > 1)
        {
            try
            {
                args.DragUIOverride.SetContentFromBitmapImage(_getEmptyDragBitmap());
                args.DragUIOverride.IsContentVisible = true;
            }
            catch
            {
                args.DragUIOverride.IsContentVisible = false;
            }
        }

        args.Handled = true;
        TryAutoScroll(args);
        return TryUpdateHover(args, itemPropertyKey);
    }

    public bool TryUpdateHover(DragEventArgs args, string itemPropertyKey)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentException.ThrowIfNullOrWhiteSpace(itemPropertyKey);
        if (!args.DataView.Properties.ContainsKey(itemPropertyKey))
        {
            return false;
        }

        UpdateHover(args, itemPropertyKey);
        return _isPreviewActive();
    }

    public TMenu? ResolveTarget(DragEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        ListView? listView = _getListView();
        if (listView == null)
        {
            return null;
        }

        TMenu? resolved = LauncherDropHitTestAdapter.FindRealizedItem<TMenu>(
            listView,
            args.GetPosition(listView));
        if (resolved != null || !IsPointerOverGroupBar(args))
        {
            return resolved;
        }

        TryAutoScroll(args);
        listView.UpdateLayout();
        return LauncherDropHitTestAdapter.FindRealizedItem<TMenu>(
            listView,
            args.GetPosition(listView));
    }

    public bool IsPointerOverGroupBar(DragEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        FrameworkElement? groupBar = _getGroupBar();
        return groupBar != null
            && LauncherDropHitTestAdapter.IsPointInside(
                groupBar,
                args.GetPosition(groupBar));
    }

    public void ClearIndicators()
    {
        foreach (TMenu menu in _getMenus())
        {
            _setIndicator(menu, false);
        }

        _hover.Clear();
    }

    public void ClearHighlight()
    {
        ClearIndicators();
        _restorePreview();
    }

    private void UpdateHover(DragEventArgs args, string itemPropertyKey)
    {
        if (!_isCustomDragActive())
        {
            return;
        }

        TItem? primary = _getPrimaryDraggedItem();
        if (primary == null
            && args.DataView.Properties.TryGetValue(itemPropertyKey, out object? value))
        {
            primary = value as TItem;
        }

        IReadOnlyList<TItem> draggedItems = _getDraggedItems();
        TMenu? target = ResolveTarget(args);
        bool canPreview = target != null
            && primary != null
            && draggedItems.Count > 0
            && _canPreview(draggedItems, target);
        if (!canPreview)
        {
            if (_isPreviewActive() && _isPointerOverShortcutArea(args))
            {
                ClearIndicators();
            }
            else if (_isPreviewActive())
            {
                ClearHighlight();
            }
            else if (IsPointerOverGroupBar(args))
            {
                ClearIndicators();
            }
            else
            {
                ClearHighlight();
            }

            return;
        }

        LauncherGroupTabHoverChange<TMenu> change = _hover.SetTarget(target);
        if (change.Changed && change.PreviousTarget != null)
        {
            _setIndicator(change.PreviousTarget, false);
        }

        _setIndicator(target!, true);
        if (!ReferenceEquals(target, _getPreviewTarget()))
        {
            _applyPreview(target!);
        }
    }

    private void TryAutoScroll(DragEventArgs args)
    {
        ScrollViewer? scrollViewer = _getScrollViewer();
        FrameworkElement? groupBar = _getGroupBar();
        if (scrollViewer == null || groupBar == null)
        {
            return;
        }

        Point position = args.GetPosition(groupBar);
        LauncherHorizontalAutoScrollPlan plan = LauncherHorizontalAutoScrollPlanner.Plan(
            position.Y,
            groupBar.ActualHeight,
            scrollViewer.VerticalOffset,
            scrollViewer.ScrollableHeight,
            edgeThreshold: 36d,
            scrollStep: 28d);
        if (plan.ShouldScroll)
        {
            scrollViewer.ChangeView(null, plan.TargetOffset, null, false);
        }
    }
}
