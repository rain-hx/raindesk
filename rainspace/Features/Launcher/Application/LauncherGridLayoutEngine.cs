namespace rainspace.Features.Launcher.Application;

internal readonly record struct LauncherPoint(double X, double Y);

internal readonly record struct LauncherRect(double X, double Y, double Width, double Height)
{
    public bool Contains(LauncherPoint point)
    {
        return point.X >= X
            && point.X <= X + Width
            && point.Y >= Y
            && point.Y <= Y + Height;
    }

    public bool IntersectsWith(LauncherRect other)
    {
        return X < other.X + other.Width
            && X + Width > other.X
            && Y < other.Y + other.Height
            && Y + Height > other.Y;
    }
}

internal readonly record struct LauncherGridCell(int Column, int Row);

internal readonly record struct LauncherGridMetrics(
    int ColumnCount,
    double SlotWidth,
    double SlotHeight);

internal readonly record struct LauncherGridItem(
    object Key,
    int ColumnSpan,
    int RowSpan,
    int Column = -1,
    int Row = -1);

internal readonly record struct LauncherGridRect(
    LauncherGridCell Cell,
    int ColumnSpan,
    int RowSpan,
    LauncherRect Bounds);

internal readonly record struct LauncherGridExtent(double Width, double Height);

internal static class LauncherGridLayoutEngine
{
    public static Dictionary<object, LauncherGridRect> Arrange(
        IEnumerable<LauncherGridItem> items,
        LauncherGridMetrics metrics)
    {
        int columnCount = Math.Max(1, metrics.ColumnCount);
        double slotWidth = Math.Max(1d, metrics.SlotWidth);
        double slotHeight = Math.Max(1d, metrics.SlotHeight);

        var occupied = new HashSet<LauncherGridCell>();
        var layout = new Dictionary<object, LauncherGridRect>();
        List<LauncherGridItem> normalizedItems = items
            .Where(item => item.Key != null)
            .ToList();

        // Reserve explicitly positioned shortcuts first so automatic items yield
        // to the user's selected grid cell regardless of collection order.
        foreach (LauncherGridItem item in normalizedItems.Where(HasManualPosition))
        {
            PlaceItem(item, occupied, layout, columnCount, slotWidth, slotHeight);
        }

        foreach (LauncherGridItem item in normalizedItems.Where(item => !HasManualPosition(item)))
        {
            PlaceItem(item, occupied, layout, columnCount, slotWidth, slotHeight);
        }

        return layout;
    }

    private static bool HasManualPosition(LauncherGridItem item)
        => item.Column >= 0 && item.Row >= 0;

    private static void PlaceItem(
        LauncherGridItem item,
        HashSet<LauncherGridCell> occupied,
        Dictionary<object, LauncherGridRect> layout,
        int columnCount,
        double slotWidth,
        double slotHeight)
    {
        int columnSpan = Math.Clamp(item.ColumnSpan, 1, columnCount);
        int rowSpan = Math.Max(1, item.RowSpan);
        LauncherGridCell cell = ResolveCell(occupied, columnCount, columnSpan, rowSpan, item);

        for (int row = cell.Row; row < cell.Row + rowSpan; row++)
        {
            for (int column = cell.Column; column < cell.Column + columnSpan; column++)
            {
                occupied.Add(new LauncherGridCell(column, row));
            }
        }

        layout[item.Key] = new LauncherGridRect(
            cell,
            columnSpan,
            rowSpan,
            new LauncherRect(
                cell.Column * slotWidth,
                cell.Row * slotHeight,
                columnSpan * slotWidth,
                rowSpan * slotHeight));
    }

    private static LauncherGridCell ResolveCell(
        HashSet<LauncherGridCell> occupied,
        int columnCount,
        int columnSpan,
        int rowSpan,
        LauncherGridItem item)
    {
        if (item.Column >= 0 && item.Row >= 0)
        {
            int column = Math.Clamp(item.Column, 0, Math.Max(0, columnCount - columnSpan));
            int row = Math.Max(0, item.Row);
            if (CanPlace(occupied, column, row, columnSpan, rowSpan))
            {
                return new LauncherGridCell(column, row);
            }
        }

        return FindFirstAvailableCell(occupied, columnCount, columnSpan, rowSpan);
    }

    public static LauncherGridExtent CalculateExtent(
        IReadOnlyDictionary<object, LauncherGridRect> layout,
        LauncherGridMetrics metrics)
    {
        int columnCount = Math.Max(1, metrics.ColumnCount);
        double slotWidth = Math.Max(1d, metrics.SlotWidth);
        double width = columnCount * slotWidth;
        double height = 0d;

        if (layout != null)
        {
            foreach (LauncherGridRect rect in layout.Values)
            {
                height = Math.Max(height, rect.Bounds.Y + rect.Bounds.Height);
            }
        }

        return new LauncherGridExtent(width, height);
    }

    private static LauncherGridCell FindFirstAvailableCell(
        HashSet<LauncherGridCell> occupied,
        int columnCount,
        int columnSpan,
        int rowSpan)
    {
        for (int row = 0; ; row++)
        {
            for (int column = 0; column <= columnCount - columnSpan; column++)
            {
                if (CanPlace(occupied, column, row, columnSpan, rowSpan))
                {
                    return new LauncherGridCell(column, row);
                }
            }
        }
    }

    private static bool CanPlace(
        HashSet<LauncherGridCell> occupied,
        int startColumn,
        int startRow,
        int columnSpan,
        int rowSpan)
    {
        for (int row = startRow; row < startRow + rowSpan; row++)
        {
            for (int column = startColumn; column < startColumn + columnSpan; column++)
            {
                if (occupied.Contains(new LauncherGridCell(column, row)))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
