using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace rainspace.Features.Workspace.Presentation.WinUI;

internal static class WorkspaceGroupTabLayoutAdapter
{
    public static bool TryGetContainer<T>(
        ListView? listView,
        T item,
        out ListViewItem container)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        container = null!;
        if (listView?.ContainerFromItem(item) is ListViewItem fromItem)
        {
            container = fromItem;
            return true;
        }

        if (listView != null)
        {
            int index = listView.Items.IndexOf(item);
            if (index >= 0 && listView.ContainerFromIndex(index) is ListViewItem fromIndex)
            {
                container = fromIndex;
                return true;
            }
        }

        return false;
    }

    public static IReadOnlyList<WorkspaceGroupTabSlot> BuildSlots<T>(
        ListView listView,
        IReadOnlyList<T> items,
        T? draggedItem)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(listView);
        ArgumentNullException.ThrowIfNull(items);
        var slots = new List<WorkspaceGroupTabSlot>();
        for (int i = 0; i < items.Count; i++)
        {
            if (!TryGetBounds(listView, i, out Rect bounds))
            {
                continue;
            }

            slots.Add(new WorkspaceGroupTabSlot(
                i,
                bounds.Y,
                bounds.Height,
                ReferenceEquals(items[i], draggedItem)));
        }

        return slots;
    }

    public static bool TryGetBounds(ListView listView, int index, out Rect bounds)
    {
        ArgumentNullException.ThrowIfNull(listView);
        bounds = default;
        if (index < 0
            || index >= listView.Items.Count
            || listView.ContainerFromIndex(index) is not ListViewItem container
            || container.Visibility == Visibility.Collapsed
            || container.ActualWidth <= 0.01d)
        {
            return false;
        }

        try
        {
            bounds = container.TransformToVisual(listView)
                .TransformBounds(new Rect(0d, 0d, container.ActualWidth, container.ActualHeight));
            return bounds.Width > 0.01d;
        }
        catch
        {
            bounds = default;
            return false;
        }
    }

    public static bool TryGetBounds<T>(
        ListView? listView,
        T item,
        UIElement relativeTo,
        out Rect bounds)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(relativeTo);
        bounds = default;
        if (!TryGetContainer(listView, item, out ListViewItem container)
            || container.Visibility == Visibility.Collapsed
            || container.ActualWidth <= 0.01d)
        {
            return false;
        }

        try
        {
            bounds = container.TransformToVisual(relativeTo)
                .TransformBounds(new Rect(0d, 0d, container.ActualWidth, container.ActualHeight));
            return bounds.Width > 0.01d;
        }
        catch
        {
            bounds = default;
            return false;
        }
    }
}
