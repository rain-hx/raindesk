using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Resolves cross-group preview placement geometry and owns dragged source-container hiding.
/// </summary>
internal sealed class LauncherCrossGroupPreviewPlacementController<TItem, TTarget>
    where TItem : class
    where TTarget : class
{
    private readonly Func<bool> _isPreviewVisible;
    private readonly Func<ListView?> _getPreviewView;
    private readonly Func<IReadOnlyCollection<TItem>> _getPreviewItems;
    private readonly Func<ListView?> _getSourceView;
    private readonly Func<IReadOnlyCollection<TItem>> _getSourceItems;
    private readonly Func<IReadOnlyList<TItem>> _getPlaceholders;
    private readonly Func<IReadOnlyList<TItem>> _getDraggedItems;
    private readonly Func<Canvas?> _getOverlay;
    private readonly Func<TTarget?> _getFallbackTarget;
    private readonly Func<TTarget, ListViewItem?> _resolveTargetContainer;
    private readonly Func<DependencyObject, FrameworkElement?> _findIconVisual;

    public LauncherCrossGroupPreviewPlacementController(
        Func<bool> isPreviewVisible,
        Func<ListView?> getPreviewView,
        Func<IReadOnlyCollection<TItem>> getPreviewItems,
        Func<ListView?> getSourceView,
        Func<IReadOnlyCollection<TItem>> getSourceItems,
        Func<IReadOnlyList<TItem>> getPlaceholders,
        Func<IReadOnlyList<TItem>> getDraggedItems,
        Func<Canvas?> getOverlay,
        Func<TTarget?> getFallbackTarget,
        Func<TTarget, ListViewItem?> resolveTargetContainer,
        Func<DependencyObject, FrameworkElement?> findIconVisual)
    {
        _isPreviewVisible = isPreviewVisible ?? throw new ArgumentNullException(nameof(isPreviewVisible));
        _getPreviewView = getPreviewView ?? throw new ArgumentNullException(nameof(getPreviewView));
        _getPreviewItems = getPreviewItems ?? throw new ArgumentNullException(nameof(getPreviewItems));
        _getSourceView = getSourceView ?? throw new ArgumentNullException(nameof(getSourceView));
        _getSourceItems = getSourceItems ?? throw new ArgumentNullException(nameof(getSourceItems));
        _getPlaceholders = getPlaceholders ?? throw new ArgumentNullException(nameof(getPlaceholders));
        _getDraggedItems = getDraggedItems ?? throw new ArgumentNullException(nameof(getDraggedItems));
        _getOverlay = getOverlay ?? throw new ArgumentNullException(nameof(getOverlay));
        _getFallbackTarget = getFallbackTarget ?? throw new ArgumentNullException(nameof(getFallbackTarget));
        _resolveTargetContainer = resolveTargetContainer
            ?? throw new ArgumentNullException(nameof(resolveTargetContainer));
        _findIconVisual = findIconVisual ?? throw new ArgumentNullException(nameof(findIconVisual));
    }

    public Point? ResolveSlot()
    {
        bool usePreview = _isPreviewVisible();
        ListView? listView = usePreview ? _getPreviewView() : _getSourceView();
        IReadOnlyCollection<TItem> hostItems = usePreview
            ? _getPreviewItems()
            : _getSourceItems();
        Canvas? overlay = _getOverlay();
        if (listView == null || overlay == null)
        {
            return ResolveFallbackTarget(overlay);
        }

        LauncherCrossGroupPreviewAnchor<TItem> anchor =
            LauncherCrossGroupPreviewAnchorSelector.Select(
                hostItems,
                _getPlaceholders(),
                _getDraggedItems());
        if (anchor.Item != null
            && TryGetItemOrigin(listView, anchor.Item, overlay, out Point origin))
        {
            return origin;
        }

        return ResolveFallbackTarget(overlay);
    }

    public bool TryAnimatePlacement(
        int token,
        Func<int, bool> isCurrent,
        bool hasPreview,
        bool animationsAllowed,
        bool hideDraggedSourceItems,
        Action<double, double, double> animate)
    {
        ArgumentNullException.ThrowIfNull(isCurrent);
        ArgumentNullException.ThrowIfNull(animate);
        if (!isCurrent(token) || !hasPreview || !animationsAllowed)
        {
            return false;
        }

        Point? slot = ResolveSlot();
        if (!slot.HasValue)
        {
            return false;
        }

        if (hideDraggedSourceItems)
        {
            HideDraggedSourceContainers();
        }

        animate(slot.Value.X, slot.Value.Y, 1d);
        return true;
    }

    private bool TryGetItemOrigin(
        ListView listView,
        TItem item,
        Canvas overlay,
        out Point origin)
    {
        origin = default;
        listView.UpdateLayout();
        try
        {
            if (listView.ContainerFromItem(item) is not ListViewItem container)
            {
                return false;
            }

            if (container.ContentTemplateRoot is DependencyObject root
                && _findIconVisual(root) is FrameworkElement iconVisual)
            {
                origin = iconVisual.TransformToVisual(overlay).TransformPoint(new Point(0d, 0d));
                return true;
            }

            origin = container.TransformToVisual(overlay).TransformPoint(new Point(0d, 0d));
            return true;
        }
        catch
        {
            origin = default;
            return false;
        }
    }

    private Point? ResolveFallbackTarget(Canvas? overlay)
    {
        TTarget? target = _getFallbackTarget();
        if (overlay == null || target == null)
        {
            return null;
        }

        ListViewItem? container = _resolveTargetContainer(target);
        if (container == null)
        {
            return null;
        }

        try
        {
            Rect bounds = container.TransformToVisual(overlay)
                .TransformBounds(new Rect(0d, 0d, container.ActualWidth, container.ActualHeight));
            return new Point(
                bounds.X + (bounds.Width / 2d),
                bounds.Y + (bounds.Height / 2d));
        }
        catch
        {
            return null;
        }
    }

    private void HideDraggedSourceContainers()
    {
        ListView? sourceView = _getSourceView();
        IReadOnlyCollection<TItem> sourceItems = _getSourceItems();
        if (sourceView == null)
        {
            return;
        }

        sourceView.UpdateLayout();
        foreach (TItem item in _getDraggedItems())
        {
            if (!sourceItems.Contains(item)
                || sourceView.ContainerFromItem(item) is not ListViewItem container)
            {
                continue;
            }

            container.Opacity = 0d;
            if (container.ContentTemplateRoot is UIElement root)
            {
                root.Opacity = 0d;
            }
        }
    }
}
