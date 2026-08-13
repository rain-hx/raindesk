using raindesk.ViewModel;
using raindesk.Features.Launcher.Application;

namespace raindesk.Features.Launcher.Presentation;

internal enum LauncherDropKind
{
    None = 0,
    FolderTarget = 1,
    CreateFolderTarget = 2,
    ReorderTarget = 3,
    GroupTarget = 4
}

internal readonly record struct LauncherDragPreview(
    LauncherDropKind Kind,
    IconInfo? TargetIcon,
    int ReorderIndex,
    bool ShouldReorder);

internal static class LauncherDragSession
{
    public static LauncherDragPreview ResolvePointer(
        IReadOnlyList<IconInfo> tileOrder,
        IReadOnlyList<IconInfo> draggedIcons,
        IReadOnlyDictionary<IconInfo, LauncherGridRect> layout,
        LauncherPoint pointer,
        int tileLimit = int.MaxValue)
    {
        if (draggedIcons == null || draggedIcons.Count == 0)
        {
            return new LauncherDragPreview(LauncherDropKind.None, null, -1, false);
        }

        IconInfo primary = draggedIcons[0];
        var draggedSet = new HashSet<IconInfo>(draggedIcons);

        foreach (var pair in layout)
        {
            IconInfo candidate = pair.Key;
            if (candidate == null || draggedSet.Contains(candidate))
            {
                continue;
            }

            LauncherRect bounds = pair.Value.Bounds;
            if (!bounds.Contains(pointer))
            {
                continue;
            }

            bool isCenterDropZone = GetDropTargetHotZone(bounds).Contains(pointer);
            if (candidate.IsFolder && CanMoveIntoFolder(primary, candidate))
            {
                if (isCenterDropZone)
                {
                    return new LauncherDragPreview(LauncherDropKind.FolderTarget, candidate, -1, false);
                }

                continue;
            }

            if (CanCreateFolder(primary, candidate))
            {
                if (isCenterDropZone)
                {
                    return new LauncherDragPreview(LauncherDropKind.CreateFolderTarget, candidate, -1, false);
                }

                continue;
            }

            if (isCenterDropZone && !primary.IsFolder)
            {
                return new LauncherDragPreview(LauncherDropKind.None, null, -1, false);
            }
        }

        return new LauncherDragPreview(
            LauncherDropKind.ReorderTarget,
            null,
            ResolveGridReorderIndex(tileOrder, layout, pointer, tileLimit, draggedSet),
            true);
    }

    public static int ResolveGridReorderIndex<T>(
        IReadOnlyList<T> tileOrder,
        IReadOnlyDictionary<T, LauncherGridRect> layout,
        LauncherPoint pointer,
        int tileLimit,
        IReadOnlySet<T>? excludedItems = null)
        where T : class
    {
        if (tileOrder == null || tileOrder.Count == 0 || layout == null || layout.Count == 0)
        {
            return 0;
        }

        var ordered = new List<(T Item, int ListIndex, LauncherGridRect Rect)>();
        for (int i = 0; i < tileOrder.Count; i++)
        {
            T item = tileOrder[i];
            if (item == null
                || excludedItems?.Contains(item) == true
                || !layout.TryGetValue(item, out LauncherGridRect rect))
            {
                continue;
            }

            ordered.Add((item, i, rect));
        }

        if (ordered.Count == 0)
        {
            return 0;
        }

        ordered.Sort(static (left, right) =>
        {
            int rowCompare = left.Rect.Cell.Row.CompareTo(right.Rect.Cell.Row);
            return rowCompare != 0
                ? rowCompare
                : left.Rect.Cell.Column.CompareTo(right.Rect.Cell.Column);
        });

        List<LauncherGridRect> allRects = layout.Values.ToList();
        double cellWidth = allRects
            .Select(rect => rect.Bounds.Width / Math.Max(1, rect.ColumnSpan))
            .Where(width => width > 0d)
            .DefaultIfEmpty(88d)
            .Min();
        double cellHeight = allRects
            .Select(rect => rect.Bounds.Height / Math.Max(1, rect.RowSpan))
            .Where(height => height > 0d)
            .DefaultIfEmpty(94d)
            .Min();
        double containerLeft = allRects.Min(rect => rect.Bounds.X - rect.Cell.Column * cellWidth);
        double containerTop = allRects.Min(rect => rect.Bounds.Y - rect.Cell.Row * cellHeight);
        int columns = Math.Max(1, allRects.Max(rect => rect.Cell.Column + rect.ColumnSpan));

        for (int i = 0; i < ordered.Count; i++)
        {
            LauncherRect bounds = ordered[i].Rect.Bounds;
            if (!bounds.Contains(pointer))
            {
                continue;
            }

            double centerX = bounds.X + bounds.Width * 0.5d;
            return Math.Clamp(
                pointer.X > centerX ? i + 1 : i,
                0,
                Math.Min(tileLimit, ordered.Count));
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            LauncherRect bounds = ordered[i].Rect.Bounds;
            bool inRowBand = pointer.Y >= bounds.Y && pointer.Y <= bounds.Y + bounds.Height;
            if (inRowBand && pointer.X < bounds.X)
            {
                return Math.Clamp(i, 0, Math.Min(tileLimit, ordered.Count));
            }
        }

        double localX = pointer.X - containerLeft;
        double localY = pointer.Y - containerTop;
        int row = Math.Max(0, (int)Math.Floor(localY / cellHeight));
        int col = Math.Clamp((int)Math.Floor(localX / cellWidth), 0, columns - 1);
        double cellLeft = col * cellWidth;
        int targetSlot = row * columns + col;
        bool afterCellCenter = localX > cellLeft + cellWidth * 0.5d;

        int insertIndex = 0;
        bool foundTargetCell = false;
        foreach (var item in ordered)
        {
            int itemSlot = item.Rect.Cell.Row * columns + item.Rect.Cell.Column;
            if (itemSlot < targetSlot)
            {
                insertIndex++;
                continue;
            }

            if (itemSlot == targetSlot)
            {
                foundTargetCell = true;
                if (afterCellCenter)
                {
                    insertIndex++;
                }
            }

            break;
        }

        if (!foundTargetCell && targetSlot > ordered[^1].Rect.Cell.Row * columns + ordered[^1].Rect.Cell.Column)
        {
            insertIndex = ordered.Count;
        }

        return Math.Clamp(insertIndex, 0, Math.Min(tileLimit, ordered.Count));
    }

    public static int ResolveListReorderIndex<T>(
        IReadOnlyList<T> tileOrder,
        IReadOnlyDictionary<T, LauncherGridRect> layout,
        LauncherPoint pointer,
        int tileLimit,
        T? skipItem = null)
        where T : class
    {
        if (tileOrder == null || tileOrder.Count == 0 || layout == null || layout.Count == 0)
        {
            return 0;
        }

        var ordered = new List<(int ListIndex, LauncherGridRect Rect)>();
        for (int i = 0; i < tileOrder.Count; i++)
        {
            T item = tileOrder[i];
            if (item == null)
            {
                continue;
            }

            if (skipItem != null && ReferenceEquals(item, skipItem))
            {
                continue;
            }

            if (!layout.TryGetValue(item, out LauncherGridRect rect))
            {
                continue;
            }

            ordered.Add((i, rect));
        }

        if (ordered.Count == 0)
        {
            return 0;
        }

        ordered.Sort(static (left, right) =>
        {
            int rowCompare = left.Rect.Cell.Row.CompareTo(right.Rect.Cell.Row);
            return rowCompare != 0
                ? rowCompare
                : left.Rect.Cell.Column.CompareTo(right.Rect.Cell.Column);
        });

        foreach ((int listIndex, LauncherGridRect rect) in ordered)
        {
            double midpointY = rect.Bounds.Y + rect.Bounds.Height / 2d;
            double midpointX = rect.Bounds.X + rect.Bounds.Width / 2d;
            if (pointer.Y < midpointY
                || (Math.Abs(pointer.Y - midpointY) < 0.01d && pointer.X < midpointX))
            {
                return listIndex;
            }
        }

        return Math.Min(ordered[^1].ListIndex + 1, tileLimit);
    }

    private static LauncherRect GetDropTargetHotZone(LauncherRect bounds)
    {
        double horizontalInset = bounds.Width * 0.175d;
        double verticalInset = bounds.Height * 0.175d;
        return new LauncherRect(
            bounds.X + horizontalInset,
            bounds.Y + verticalInset,
            Math.Max(0d, bounds.Width - horizontalInset * 2d),
            Math.Max(0d, bounds.Height - verticalInset * 2d));
    }

    private static bool CanCreateFolder(IconInfo dragged, IconInfo target) =>
        LauncherShortcutDropTraitsAdapter.CanCreateFolderFrom(dragged, target);

    private static bool CanMoveIntoFolder(IconInfo dragged, IconInfo target) =>
        LauncherShortcutDropTraitsAdapter.CanMoveIntoFolder(dragged, target);
}
