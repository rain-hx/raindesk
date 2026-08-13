using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace rainspace.Features.Launcher.Presentation.WinUI;

internal static class LauncherShortcutViewportAdapter
{
    public static double GetAvailableWidth(
        ListView? listView,
        Grid? host,
        FrameworkElement? root,
        double rootHorizontalInset)
    {
        double listContentWidth = GetListContentWidth(listView);
        if (listContentWidth > 0d)
        {
            return listContentWidth;
        }

        double hostWidth = host?.ActualWidth > 0d
            ? Math.Max(0d, host.ActualWidth - host.Padding.Left - host.Padding.Right)
            : 0d;
        double rootWidth = root?.ActualWidth > 0d
            ? Math.Max(0d, root.ActualWidth - Math.Max(0d, rootHorizontalInset))
            : 0d;

        if (hostWidth > 0d && rootWidth > 0d)
        {
            return Math.Min(hostWidth, rootWidth);
        }

        return Math.Max(hostWidth, rootWidth);
    }

    private static double GetListContentWidth(ListView? listView)
    {
        if (listView == null)
        {
            return 0d;
        }

        ScrollViewer? scrollViewer = FindDescendantScrollViewer(listView);
        double width = scrollViewer?.ViewportWidth > 0d
            ? scrollViewer.ViewportWidth
            : listView.ActualWidth;
        if (width <= 0d)
        {
            return 0d;
        }

        width = Math.Max(0d, width - listView.Padding.Left - listView.Padding.Right);
        if (scrollViewer != null && RequiresVerticalScrollbarReservation(scrollViewer))
        {
            width = Math.Max(0d, width - GetVerticalScrollbarOccupiedWidth(scrollViewer));
        }

        return width;
    }

    private static bool RequiresVerticalScrollbarReservation(ScrollViewer scrollViewer) =>
        scrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible
        || scrollViewer.ScrollableHeight > 0.5d;

    private static double GetVerticalScrollbarOccupiedWidth(ScrollViewer scrollViewer)
    {
        if (!RequiresVerticalScrollbarReservation(scrollViewer))
        {
            return 0d;
        }

        double reserved = scrollViewer.ActualWidth - scrollViewer.ViewportWidth;
        return reserved > 0.5d ? reserved : 17d;
    }

    private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
    {
        int childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
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
}
