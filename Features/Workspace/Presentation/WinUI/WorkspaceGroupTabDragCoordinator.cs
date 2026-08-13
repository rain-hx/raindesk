using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace raindesk.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Coordinates the complete WinUI gesture lifecycle for manual workspace tab reordering.
/// Business persistence remains an explicit callback owned by the page/application boundary.
/// </summary>
internal sealed class WorkspaceGroupTabDragCoordinator<TMenu>
    where TMenu : class
{
    private readonly WorkspaceGroupTabDragSessionState<TMenu> _state = new();
    private readonly WorkspaceGroupTabDragPreviewController _preview = new();
    private readonly WorkspaceGroupTabPointerTrackingController _pointerTracking = new();
    private readonly WorkspaceGroupTabDragVisualController _visual = new();
    private readonly Func<ListView?> _getListView;
    private readonly Func<ScrollViewer?> _getScrollViewer;
    private readonly Func<Canvas?> _getOverlay;
    private readonly Func<UIElement?> _getWindowRoot;
    private readonly Func<IList<TMenu>> _getItems;
    private readonly Func<bool> _canStart;
    private readonly Func<ListViewItem, TMenu?> _resolveMenu;
    private readonly Func<TMenu, bool> _isFixedTab;
    private readonly Action _commitOrder;
    private readonly WorkspaceGroupTabWidthMetrics _widthMetrics;
    private readonly double _activationThreshold;
    private readonly double _insertHysteresis;
    private readonly TimeSpan _repositionDuration;
    private ListViewItem? _dragContainer;
    private ScrollMode? _savedVerticalScrollMode;

    public WorkspaceGroupTabDragCoordinator(
        Func<ListView?> getListView,
        Func<ScrollViewer?> getScrollViewer,
        Func<Canvas?> getOverlay,
        Func<UIElement?> getWindowRoot,
        Func<IList<TMenu>> getItems,
        Func<bool> canStart,
        Func<ListViewItem, TMenu?> resolveMenu,
        Func<TMenu, bool> isFixedTab,
        Action commitOrder,
        WorkspaceGroupTabWidthMetrics widthMetrics,
        double activationThreshold = 8d,
        double insertHysteresis = 14d)
    {
        _getListView = getListView ?? throw new ArgumentNullException(nameof(getListView));
        _getScrollViewer = getScrollViewer ?? throw new ArgumentNullException(nameof(getScrollViewer));
        _getOverlay = getOverlay ?? throw new ArgumentNullException(nameof(getOverlay));
        _getWindowRoot = getWindowRoot ?? throw new ArgumentNullException(nameof(getWindowRoot));
        _getItems = getItems ?? throw new ArgumentNullException(nameof(getItems));
        _canStart = canStart ?? throw new ArgumentNullException(nameof(canStart));
        _resolveMenu = resolveMenu ?? throw new ArgumentNullException(nameof(resolveMenu));
        _isFixedTab = isFixedTab ?? throw new ArgumentNullException(nameof(isFixedTab));
        _commitOrder = commitOrder ?? throw new ArgumentNullException(nameof(commitOrder));
        _widthMetrics = widthMetrics;
        _activationThreshold = Math.Max(0d, activationThreshold);
        _insertHysteresis = Math.Max(0d, insertHysteresis);
        _repositionDuration = TimeSpan.FromMilliseconds(180);

        PointerPressedHandler = HandlePointerPressed;
        PointerMovedHandler = HandlePointerMoved;
        PointerReleasedHandler = HandlePointerReleased;
        PointerCanceledHandler = HandlePointerCanceled;
    }

    public PointerEventHandler PointerPressedHandler { get; }

    public PointerEventHandler PointerMovedHandler { get; }

    public PointerEventHandler PointerReleasedHandler { get; }

    public PointerEventHandler PointerCanceledHandler { get; }

    public TMenu? Menu => _state.Menu;

    public bool ConsumeSuppressedClick() => _state.ConsumeSuppressedClick();

    public void SetReorderEnabled(bool enabled) => _state.SetReorderEnabled(enabled);

    public void SuspendReorder() => _state.SuspendReorder();

    public void RestoreReorder(bool allowed) => _state.RestoreReorder(allowed);

    public void ApplyPlaceholderChrome(ListViewItem item) =>
        _visual.ApplyPlaceholderChrome(item);

    public void EnableDeleteTransitions(ListView listView, TimeSpan duration) =>
        _visual.EnableDeleteTransitions(listView, duration);

    public void RestoreTransitions(ListView listView) =>
        _visual.RestoreTransitions(listView);

    public void Abort()
    {
        if (_state.HasSession && _state.WillCommitMove)
        {
            MoveItem(_state.CurrentIndex, _state.OriginalIndex);
        }

        CleanupInteraction();
        _state.Complete();
    }

    private void HandlePointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ListView? listView = _getListView();
        if (listView == null
            || !_state.IsReorderEnabled
            || !_canStart()
            || sender is not ListViewItem item)
        {
            return;
        }

        TMenu? menu = _resolveMenu(item);
        if (menu == null || _isFixedTab(menu))
        {
            return;
        }

        _dragContainer = item;
        int sourceIndex = _getItems().IndexOf(menu);
        Point start = e.GetCurrentPoint(listView).Position;
        _state.Begin(menu, sourceIndex, start.X, start.Y);
        item.CapturePointer(e.Pointer);
        BeginWindowPointerTracking();
        SuppressVerticalScroll();
        e.Handled = true;
    }

    private void HandlePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        ListView? listView = _getListView();
        if (!_state.HasSession || listView == null)
        {
            return;
        }

        Point current = e.GetCurrentPoint(listView).Position;
        WorkspaceGroupTabDragActivation activation = _state.TryActivate(
            current.X,
            current.Y,
            _activationThreshold);
        if (!activation.IsActive)
        {
            return;
        }

        if (activation.ActivatedNow)
        {
            _visual.EnableRepositionTransitions(listView, _repositionDuration);
            _ = BeginPreviewAsync(current);
        }

        if (_preview.HasPreview)
        {
            UpdatePreviewPosition(current);
        }

        int targetIndex = ResolveInsertIndex(current);
        if (targetIndex < 1 || _state.CurrentIndex < 1)
        {
            return;
        }

        if (_state.TrySetPendingTarget(targetIndex)
            && targetIndex != _state.CurrentIndex)
        {
            MoveItem(_state.CurrentIndex, targetIndex);
            _state.CommitLiveMove(targetIndex);
            HideDragContainer();
        }

        e.Handled = true;
    }

    private void HandlePointerReleased(object sender, PointerRoutedEventArgs e) =>
        Complete(sender, e);

    private void HandlePointerCanceled(object sender, PointerRoutedEventArgs e) =>
        Complete(sender, e);

    private void Complete(object sender, PointerRoutedEventArgs e)
    {
        if (!_state.HasSession)
        {
            return;
        }

        if (sender is ListViewItem item)
        {
            item.ReleasePointerCapture(e.Pointer);
        }

        if (_state.WillCommitMove)
        {
            _commitOrder();
        }

        CleanupInteraction();
        WorkspaceGroupTabDragCompletion completion = _state.Complete();
        e.Handled = completion.Moved;
    }

    private void BeginWindowPointerTracking()
    {
        UIElement? root = _getWindowRoot();
        if (root != null)
        {
            _pointerTracking.Begin(root, HandleWindowPointerMoved, HandleWindowPointerReleased);
        }
    }

    private void HandleWindowPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_dragContainer != null)
        {
            HandlePointerMoved(_dragContainer, e);
        }
    }

    private void HandleWindowPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_dragContainer != null)
        {
            Complete(_dragContainer, e);
        }
    }

    private void SuppressVerticalScroll()
    {
        ScrollViewer? scrollViewer = _getScrollViewer();
        if (scrollViewer == null)
        {
            return;
        }

        _savedVerticalScrollMode ??= scrollViewer.VerticalScrollMode;
        scrollViewer.VerticalScrollMode = ScrollMode.Disabled;
    }

    private void RestoreVerticalScroll()
    {
        ScrollViewer? scrollViewer = _getScrollViewer();
        if (scrollViewer != null && _savedVerticalScrollMode.HasValue)
        {
            scrollViewer.VerticalScrollMode = _savedVerticalScrollMode.Value;
        }

        _savedVerticalScrollMode = null;
    }

    private int ResolveInsertIndex(Point position)
    {
        ListView? listView = _getListView();
        IList<TMenu> items = _getItems();
        if (listView == null || items.Count <= 1)
        {
            return -1;
        }

        IReadOnlyList<TMenu> readOnlyItems = items as IReadOnlyList<TMenu>
            ?? items.ToArray();
        IReadOnlyList<WorkspaceGroupTabSlot> slots =
            WorkspaceGroupTabLayoutAdapter.BuildSlots(
                listView,
                readOnlyItems,
                _state.Menu);
        return WorkspaceGroupTabInsertPlanner.Resolve(
            position.Y,
            items.Count,
            slots,
            _state.IsActive,
            _state.CurrentIndex,
            _insertHysteresis);
    }

    private void MoveItem(int fromIndex, int toIndex)
    {
        IList<TMenu> items = _getItems();
        if (fromIndex < 1
            || toIndex < 1
            || fromIndex >= items.Count
            || toIndex >= items.Count
            || fromIndex == toIndex)
        {
            return;
        }

        TMenu item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);
    }

    private void HideDragContainer()
    {
        ListView? listView = _getListView();
        if (_state.Menu != null
            && WorkspaceGroupTabLayoutAdapter.TryGetContainer(
                listView,
                _state.Menu,
                out ListViewItem container))
        {
            _dragContainer = container;
            _visual.CapturePlaceholderHeight(container);
            _visual.ApplyPlaceholderChrome(container);
        }
    }

    private void RestoreDragContainer()
    {
        if (_dragContainer != null)
        {
            RestoreContainer(_dragContainer);
        }

        ListView? listView = _getListView();
        if (_state.Menu != null
            && WorkspaceGroupTabLayoutAdapter.TryGetContainer(
                listView,
                _state.Menu,
                out ListViewItem currentContainer)
            && !ReferenceEquals(currentContainer, _dragContainer))
        {
            RestoreContainer(currentContainer);
            _dragContainer = currentContainer;
        }

        _visual.EndInteraction();
    }

    private void RestoreContainer(ListViewItem item)
    {
        _visual.RestoreContainer(
            item,
            container => WorkspaceGroupTabSizingAdapter.ApplyNaturalWidth(
                container,
                _widthMetrics));
    }

    private async System.Threading.Tasks.Task BeginPreviewAsync(Point pointerInListView)
    {
        ListView? listView = _getListView();
        Canvas? overlay = _getOverlay();
        ListViewItem? captureTarget = _dragContainer;
        if (_preview.HasPreview
            || captureTarget == null
            || listView == null
            || overlay == null)
        {
            return;
        }

        double height = _visual.CapturePlaceholderHeight(captureTarget);
        double width = captureTarget.ActualWidth > 0d
            ? captureTarget.ActualWidth
            : 72d;
        bool began = await _preview.BeginAsync(
            captureTarget,
            overlay,
            width,
            height);
        if (began && _state.HasSession)
        {
            HideDragContainer();
            UpdatePreviewPosition(pointerInListView);
        }
    }

    private void UpdatePreviewPosition(Point pointerInListView)
    {
        ListView? listView = _getListView();
        Canvas? overlay = _getOverlay();
        if (!_preview.HasPreview || listView == null || overlay == null)
        {
            return;
        }

        Point pointerInOverlay = listView.TransformToVisual(overlay)
            .TransformPoint(pointerInListView);
        _preview.UpdatePosition(pointerInOverlay.X, pointerInOverlay.Y);
    }

    private void CleanupInteraction()
    {
        _preview.End();
        _pointerTracking.End();
        RestoreVerticalScroll();
        ListView? listView = _getListView();
        if (listView != null)
        {
            _visual.RestoreTransitions(listView);
        }

        RestoreDragContainer();
        _dragContainer = null;
    }
}
