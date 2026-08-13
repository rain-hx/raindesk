using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.Launcher.Application;
using raindesk.ViewModel;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// 将 WinUI 坐标、面板原点和已实现容器转换为 Launcher 的纯命中模型。
/// </summary>
internal static class LauncherDropHitTestAdapter
{
    private const double CenterHotZoneInsetRatio = 0.175d;

    public static Dictionary<IconInfo, LauncherGridRect> BuildLogicalLayout(
        ListView listView,
        IReadOnlyList<IconInfo> tiles,
        LauncherGridMetrics metrics,
        bool synchronizeLayout,
        Predicate<IconInfo> include)
    {
        ArgumentNullException.ThrowIfNull(listView);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(include);

        if (synchronizeLayout)
        {
            listView.UpdateLayout();
        }

        bool preserveManualPositions = listView.ItemsPanelRoot is not LauncherGridPanel launcherPanel
            || launcherPanel.UseManualPositions;
        var gridItems = tiles
            .Where(icon => icon != null && include(icon))
            .Select(icon => LauncherShortcutGridItemAdapter.Create(
                icon,
                metrics.ColumnCount,
                preserveManualPositions));
        Dictionary<object, LauncherGridRect> stableLayout =
            LauncherGridLayoutEngine.Arrange(gridItems, metrics);
        Point panelOrigin = GetPanelOrigin(listView);
        var layout = new Dictionary<IconInfo, LauncherGridRect>();
        foreach (KeyValuePair<object, LauncherGridRect> pair in stableLayout)
        {
            if (pair.Key is not IconInfo icon)
            {
                continue;
            }

            LauncherGridRect rect = pair.Value;
            layout[icon] = rect with
            {
                Bounds = new LauncherRect(
                    rect.Bounds.X + panelOrigin.X,
                    rect.Bounds.Y + panelOrigin.Y,
                    rect.Bounds.Width,
                    rect.Bounds.Height)
            };
        }

        return layout;
    }

    public static bool TryTranslatePoint(
        UIElement source,
        UIElement target,
        Point sourcePoint,
        out Point targetPoint)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        try
        {
            targetPoint = source.TransformToVisual(target).TransformPoint(sourcePoint);
            return true;
        }
        catch
        {
            targetPoint = default;
            return false;
        }
    }

    public static bool TryGetItemBounds(
        ListView listView,
        object item,
        out Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(listView);
        ArgumentNullException.ThrowIfNull(item);
        bounds = default;
        if (listView.ContainerFromItem(item) is not ListViewItem container)
        {
            return false;
        }

        try
        {
            bounds = container.TransformToVisual(listView)
                .TransformBounds(new Rect(0d, 0d, container.ActualWidth, container.ActualHeight));
            return bounds.Width > 0d && bounds.Height > 0d;
        }
        catch
        {
            bounds = default;
            return false;
        }
    }

    public static bool IsPointInsideItem(ListView listView, object item, Point point) =>
        TryGetItemBounds(listView, item, out Rect bounds) && bounds.Contains(point);

    public static bool IsPointInside(FrameworkElement element, Point point)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.ActualWidth > 0d
            && element.ActualHeight > 0d
            && point.X >= 0d
            && point.Y >= 0d
            && point.X <= element.ActualWidth
            && point.Y <= element.ActualHeight;
    }

    public static T? FindRealizedItem<T>(ListView listView, Point point)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(listView);
        for (int i = 0; i < listView.Items.Count; i++)
        {
            object? item = listView.Items[i];
            if (item is T match
                && TryGetItemBounds(listView, item, out Rect bounds)
                && bounds.Contains(point))
            {
                return match;
            }
        }

        return null;
    }

    public static bool IsPointInsideCenterHotZone(ListView listView, object item, Point point) =>
        TryGetItemBounds(listView, item, out Rect bounds)
        && GetCenterHotZone(bounds).Contains(point);

    public static IconInfo? FindCenteredTarget(
        ListView listView,
        IReadOnlyList<IconInfo> candidates,
        Point point,
        Predicate<IconInfo> canTarget)
    {
        ArgumentNullException.ThrowIfNull(listView);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(canTarget);

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            IconInfo candidate = candidates[i];
            if (candidate == null
                || !canTarget(candidate)
                || !TryGetItemBounds(listView, candidate, out Rect bounds)
                || !GetCenterHotZone(bounds).Contains(point))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    public static int ResolveRealizedListDropIndex(
        ListView listView,
        IReadOnlyList<IconInfo> tiles,
        Point point,
        int tileLimit,
        bool synchronizeLayout,
        Predicate<IconInfo> exclude)
    {
        ArgumentNullException.ThrowIfNull(listView);
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(exclude);
        if (synchronizeLayout)
        {
            listView.UpdateLayout();
        }

        int safeLimit = Math.Clamp(tileLimit, 0, tiles.Count);
        var targets = new List<ShortcutSlotHitTarget>();
        int lastIndex = Math.Min(safeLimit, tiles.Count - 1);
        for (int i = 0; i <= lastIndex; i++)
        {
            IconInfo icon = tiles[i];
            if (icon == null
                || exclude(icon)
                || !TryGetItemBounds(listView, icon, out Rect bounds))
            {
                continue;
            }

            targets.Add(new ShortcutSlotHitTarget(
                i,
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height));
        }

        return Math.Clamp(
            ShortcutLayoutCalculator.ResolveWrapGridDropIndex(
                targets,
                point.X,
                point.Y,
                safeLimit),
            0,
            safeLimit);
    }

    private static Rect GetCenterHotZone(Rect bounds)
    {
        double horizontalInset = bounds.Width * CenterHotZoneInsetRatio;
        double verticalInset = bounds.Height * CenterHotZoneInsetRatio;
        return new Rect(
            bounds.X + horizontalInset,
            bounds.Y + verticalInset,
            Math.Max(0d, bounds.Width - horizontalInset * 2d),
            Math.Max(0d, bounds.Height - verticalInset * 2d));
    }

    private static Point GetPanelOrigin(ListView listView)
    {
        if (listView.ItemsPanelRoot is not UIElement panel)
        {
            return default;
        }

        try
        {
            return panel.TransformToVisual(listView).TransformPoint(default);
        }
        catch
        {
            return default;
        }
    }
}
