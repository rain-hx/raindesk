using System;

namespace raindesk.Features.Launcher.Application;

internal readonly record struct ShortcutLayoutMetrics(
    int ColumnCount,
    double SlotWidth,
    double SlotHeight,
    double CardWidth,
    double GapWidth,
    double HorizontalOffset = 0d);

internal readonly record struct ShortcutLayoutSnapshot(
    double AvailableWidth,
    int TileCount,
    int ColumnCount,
    double SlotWidth,
    double CardWidth,
    double GapWidth,
    double HorizontalOffset,
    double SlotHeight);

internal readonly record struct BatchDragRestoreResult<T>(
    IReadOnlyList<T> Items,
    int InsertIndex);

internal readonly record struct ShortcutTileDisplaySize(double Width, double Height);

internal readonly record struct ShortcutSlotHitTarget(
    int ListIndex,
    double Left,
    double Top,
    double Width,
    double Height);

internal static class ShortcutLayoutCalculator
{
    private const double MinSlotWidth = 104d;
    private const double MinCardWidth = 88d;
    private const double MaxCardWidth = 88d;
    private const double MinGapWidth = 8d;
    private const double MaxGapWidth = 24d;
    private const double MinCompressedGapWidth = 4d;
    private const double DefaultSlotSize = 104d;
    private const int MaxColumns = 12;

    public static ShortcutLayoutMetrics Calculate(double availableWidth)
    {
        if (availableWidth <= 0d)
        {
            return new ShortcutLayoutMetrics(1, DefaultSlotSize, DefaultSlotSize, MinCardWidth, 0d);
        }

        int columnCount = CalculateOptimalColumns(availableWidth);
        return BuildMetrics(availableWidth, columnCount);
    }

    public static ShortcutTileDisplaySize CalculateTileDisplaySize(
        ShortcutLayoutMetrics metrics,
        int columnSpan,
        int rowSpan)
    {
        int safeColumnSpan = Math.Max(1, columnSpan);
        int safeRowSpan = Math.Max(1, rowSpan);
        return new ShortcutTileDisplaySize(
            metrics.SlotWidth * safeColumnSpan,
            metrics.SlotHeight * safeRowSpan);
    }

    public static int ClampFolderColumnSpan(int columnSpan, int columnCount)
    {
        return Math.Clamp(columnSpan, 1, Math.Max(1, columnCount));
    }

    public static ShortcutTileDisplaySize CalculateFolderTileDisplaySize(
        ShortcutLayoutMetrics metrics,
        int columnSpan,
        int rowSpan)
    {
        int safeColumnSpan = ClampFolderColumnSpan(columnSpan, metrics.ColumnCount);
        int safeRowSpan = Math.Max(1, rowSpan);
        return CalculateTileDisplaySize(metrics, safeColumnSpan, safeRowSpan);
    }

    public static ShortcutTileDisplaySize CalculatePanelTileDisplaySize(
        ShortcutLayoutMetrics metrics,
        int columnSpan,
        int rowSpan,
        bool supportsVariableSizedItems)
    {
        if (!supportsVariableSizedItems)
        {
            return CalculateTileDisplaySize(metrics, 1, 1);
        }

        return CalculateFolderTileDisplaySize(metrics, columnSpan, rowSpan);
    }

    public static double GetFolderBodyHeight(
        double slotHeight,
        int rowSpan,
        double labelBlockHeight = 18d,
        double stackSpacing = 4d)
    {
        int safeRowSpan = Math.Max(1, rowSpan);
        if (safeRowSpan == 1)
        {
            return 64d;
        }

        return Math.Max(54d, safeRowSpan * slotHeight - labelBlockHeight - stackSpacing);
    }

    public static double GetFolderStackPanelHeight(double slotHeight, int rowSpan)
        => GetFolderBodyHeight(slotHeight, rowSpan) + 4d + 18d;

    public static bool HasMeaningfulLayoutChange(
        ShortcutLayoutMetrics metrics,
        ShortcutLayoutSnapshot previous,
        double availableWidth,
        int tileCount)
    {
        return metrics.ColumnCount != previous.ColumnCount
            || Math.Abs(metrics.SlotWidth - previous.SlotWidth) >= 1d
            || Math.Abs(metrics.CardWidth - previous.CardWidth) >= 1d
            || Math.Abs(metrics.GapWidth - previous.GapWidth) >= 1d
            || Math.Abs(metrics.SlotHeight - previous.SlotHeight) >= 1d
            || Math.Abs(availableWidth - previous.AvailableWidth) >= 1d
            || Math.Abs(metrics.HorizontalOffset - previous.HorizontalOffset) >= 0.5d
            || tileCount != previous.TileCount;
    }

    public static IReadOnlyList<T> MovePlaceholderBlock<T>(
        IReadOnlyList<T> currentItems,
        IReadOnlyList<T> placeholderOrder,
        T movedPlaceholder,
        int newIndex)
    {
        if (currentItems.Count == 0 || placeholderOrder.Count == 0)
        {
            return currentItems.ToList();
        }

        int movedOffset = IndexOf(placeholderOrder, movedPlaceholder);
        if (movedOffset < 0)
        {
            return currentItems.ToList();
        }

        int targetBlockStart = Math.Clamp(
            newIndex - movedOffset,
            0,
            Math.Max(0, currentItems.Count - placeholderOrder.Count));
        var placeholderSet = placeholderOrder.ToHashSet();
        var withoutPlaceholders = new List<T>(currentItems.Count - placeholderOrder.Count);

        for (int i = 0; i < currentItems.Count; i++)
        {
            T item = currentItems[i];
            if (placeholderSet.Contains(item))
            {
                continue;
            }

            withoutPlaceholders.Add(item);
        }

        int insertIndex = 0;
        int slotsBeforeBlock = targetBlockStart;
        for (int i = 0; i < currentItems.Count && slotsBeforeBlock > 0; i++)
        {
            if (!placeholderSet.Contains(currentItems[i]))
            {
                insertIndex++;
                slotsBeforeBlock--;
            }
        }

        insertIndex = Math.Clamp(insertIndex, 0, withoutPlaceholders.Count);
        withoutPlaceholders.InsertRange(insertIndex, placeholderOrder);
        return withoutPlaceholders;
    }

    public static int ResolvePlaceholderBlockLogicalTargetIndex<T>(
        IReadOnlyList<T> currentItems,
        IReadOnlyCollection<T> placeholders,
        int visualTargetIndex)
    {
        if (currentItems.Count == 0 || placeholders.Count == 0)
        {
            return Math.Clamp(visualTargetIndex, 0, currentItems.Count);
        }

        int safeTargetIndex = Math.Clamp(visualTargetIndex, 0, currentItems.Count);
        int logicalIndex = 0;
        for (int i = 0; i < safeTargetIndex; i++)
        {
            if (!placeholders.Contains(currentItems[i]))
            {
                logicalIndex++;
            }
        }

        return logicalIndex;
    }

    /// <summary>
    /// Resolves a list insert index from pointer position against wrap-grid slot footprints.
    /// Targets must be in ascending list-index order; each footprint uses one cell size (not overflow).
    /// </summary>
    public static int ResolveWrapGridDropIndex(
        IReadOnlyList<ShortcutSlotHitTarget> targets,
        double pointerX,
        double pointerY,
        int tileLimit)
    {
        if (targets == null || targets.Count == 0)
        {
            return 0;
        }

        int insertAfterIndex = tileLimit;
        foreach (ShortcutSlotHitTarget target in targets)
        {
            if (target.ListIndex > tileLimit)
            {
                break;
            }

            if (pointerY < target.Top)
            {
                return target.ListIndex;
            }

            bool inRow = pointerY >= target.Top && pointerY < target.Top + target.Height;
            double midpointX = target.Left + target.Width * 0.5d;
            if (inRow && pointerX < midpointX)
            {
                return target.ListIndex;
            }

            if (inRow && pointerX >= midpointX)
            {
                insertAfterIndex = target.ListIndex + 1;
            }
        }

        return Math.Min(insertAfterIndex, tileLimit);
    }

    public static bool CanDropOnGroupTab<TIcon, TGroup>(
        IEnumerable<TIcon> icons,
        TGroup targetGroup,
        bool isAllGroupsShortcutView,
        Func<TIcon, TGroup?> findGroup)
        where TIcon : class
        where TGroup : class
    {
        if (targetGroup == null)
        {
            return false;
        }

        bool canMoveBetweenGroups = false;
        bool hasUnknownSource = false;
        foreach (TIcon icon in icons)
        {
            if (icon == null)
            {
                continue;
            }

            TGroup? sourceGroup = findGroup(icon);
            if (sourceGroup == null)
            {
                hasUnknownSource = true;
                continue;
            }

            if (!EqualityComparer<TGroup>.Default.Equals(sourceGroup, targetGroup))
            {
                canMoveBetweenGroups = true;
                break;
            }
        }

        return isAllGroupsShortcutView
            ? canMoveBetweenGroups && !hasUnknownSource
            : canMoveBetweenGroups;
    }

    public static IReadOnlyList<TIcon> FilterValidDragItems<TIcon>(
        IEnumerable<TIcon?>? icons,
        Func<TIcon, bool> isPlaceholder)
        where TIcon : class
    {
        if (icons == null)
        {
            return Array.Empty<TIcon>();
        }

        var validIcons = new List<TIcon>();
        foreach (TIcon? icon in icons)
        {
            if (icon == null || isPlaceholder(icon))
            {
                continue;
            }

            validIcons.Add(icon);
        }

        return validIcons;
    }

    public static BatchDragRestoreResult<T> BuildBatchDragRestoreItems<T>(
        IReadOnlyList<T>? dragStartOrder,
        IReadOnlyList<T>? beforePreviewOrder,
        IReadOnlyList<T>? currentOrder,
        IEnumerable<T> draggedItems,
        IReadOnlyList<T> placeholders,
        int sourceInsertIndex,
        int fallbackInsertIndex,
        Func<T, bool> isPlaceholder)
    {
        var draggedSet = draggedItems?.ToHashSet() ?? new HashSet<T>();
        List<T> items;

        if (dragStartOrder is { Count: > 0 })
        {
            items = dragStartOrder
                .Where(item => item != null && !isPlaceholder(item) && !draggedSet.Contains(item))
                .ToList();
        }
        else if (beforePreviewOrder is { Count: > 0 })
        {
            items = new List<T>(beforePreviewOrder);
        }
        else
        {
            items = (currentOrder ?? Array.Empty<T>())
                .Where(item => item != null && !draggedSet.Contains(item))
                .ToList();
        }

        if (placeholders.Count == 0)
        {
            return new BatchDragRestoreResult<T>(items, -1);
        }

        int insertAt = Math.Clamp(
            sourceInsertIndex >= 0 ? sourceInsertIndex : fallbackInsertIndex,
            0,
            items.Count);
        items.InsertRange(insertAt, placeholders);
        return new BatchDragRestoreResult<T>(items, insertAt);
    }

    private static ShortcutLayoutMetrics BuildMetrics(double availableWidth, int columnCount)
    {
        double slotWidth = availableWidth / columnCount;
        ComputeCardAndGap(slotWidth, out double cardWidth, out double gapWidth);
        // 每个启动器项目使用统一正方形槽位，图标与标题作为一个整体排列。
        return new ShortcutLayoutMetrics(columnCount, slotWidth, slotWidth, cardWidth, gapWidth, 0d);
    }

    private static int CalculateOptimalColumns(double availableWidth)
    {
        int maxColumns = Math.Min(MaxColumns, (int)Math.Floor(availableWidth / MinSlotWidth));
        maxColumns = Math.Max(1, maxColumns);

        for (int cols = maxColumns; cols >= 1; cols--)
        {
            double slotWidth = availableWidth / cols;
            ComputeCardAndGap(slotWidth, out double cardWidth, out _);
            if (cols == 1 || slotWidth >= MinSlotWidth || slotWidth - cardWidth >= MinCompressedGapWidth)
            {
                return cols;
            }
        }

        return 1;
    }

    private static void ComputeCardAndGap(double slotWidth, out double cardWidth, out double gapWidth)
    {
        if (slotWidth <= MinCardWidth + MinCompressedGapWidth)
        {
            cardWidth = MinCardWidth;
            gapWidth = Math.Max(0d, slotWidth - cardWidth);
            return;
        }

        if (slotWidth <= MaxCardWidth + MinGapWidth)
        {
            gapWidth = MinGapWidth;
            cardWidth = Math.Clamp(slotWidth - gapWidth, MinCardWidth, MaxCardWidth);
            return;
        }

        cardWidth = MaxCardWidth;
        gapWidth = Math.Min(MaxGapWidth, slotWidth - cardWidth);
    }

    private static int IndexOf<T>(IReadOnlyList<T> items, T value)
    {
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < items.Count; i++)
        {
            if (comparer.Equals(items[i], value))
            {
                return i;
            }
        }

        return -1;
    }
}
