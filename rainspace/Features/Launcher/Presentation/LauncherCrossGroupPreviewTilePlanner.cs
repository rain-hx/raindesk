namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherCrossGroupPreviewTilePlan<T>(
    IReadOnlyList<T> Items,
    int TargetInsertIndex);

/// <summary>
/// Builds a target-group preview without mutating either source collection.
/// </summary>
internal static class LauncherCrossGroupPreviewTilePlanner
{
    public static LauncherCrossGroupPreviewTilePlan<T> Plan<T>(
        IEnumerable<T>? targetItems,
        IReadOnlyList<T> draggedItems,
        Func<T, bool> exclude)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(draggedItems);
        ArgumentNullException.ThrowIfNull(exclude);

        var draggedSet = new HashSet<T>(
            draggedItems.Where(item => item != null && !exclude(item)));
        var items = new List<T>();
        if (targetItems != null)
        {
            foreach (T item in targetItems)
            {
                if (item != null && !exclude(item) && !draggedSet.Contains(item))
                {
                    items.Add(item);
                }
            }
        }

        int targetInsertIndex = items.Count;
        foreach (T item in draggedItems)
        {
            if (item != null && !exclude(item))
            {
                items.Add(item);
            }
        }

        return new LauncherCrossGroupPreviewTilePlan<T>(items, targetInsertIndex);
    }
}
