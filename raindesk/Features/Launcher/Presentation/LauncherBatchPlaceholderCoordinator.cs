using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// 编排批量拖拽占位块的创建、跨列表预览、提交与回滚。
/// 只修改调用方提供的列表，不依赖 WinUI 控件或持久化。
/// </summary>
internal sealed class LauncherBatchPlaceholderCoordinator
{
    public LauncherBatchDragSessionState State { get; } = new();

    public bool TryBegin(IList<IconInfo> tiles, IReadOnlyList<IconInfo> draggedIcons)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(draggedIcons);

        if (State.IsSuppressedFromTiles)
        {
            return false;
        }

        State.Reset();
        var captured = new List<(int Index, IconInfo Icon)>();
        foreach (IconInfo icon in draggedIcons)
        {
            if (icon == null || State.OriginalIndices.ContainsKey(icon))
            {
                continue;
            }

            int index = tiles.IndexOf(icon);
            if (index < 0)
            {
                continue;
            }

            State.OriginalIndices[icon] = index;
            captured.Add((index, icon));
        }

        if (captured.Count == 0)
        {
            State.Reset();
            return false;
        }

        captured.Sort(static (left, right) => left.Index.CompareTo(right.Index));
        foreach ((int index, _) in captured.OrderByDescending(item => item.Index))
        {
            tiles.RemoveAt(index);
        }

        int insertAt = captured[0].Index;
        int groupCount = captured.Count;
        for (int i = 0; i < captured.Count; i++)
        {
            IconInfo placeholder = CreatePlaceholder(captured[i].Icon, groupCount);
            State.YieldPlaceholders.Add(placeholder);
            tiles.Insert(insertAt + i, placeholder);
        }

        State.InsertIndex = insertAt;
        State.ResetMoveThrottle();
        State.IsSuppressedFromTiles = true;
        return true;
    }

    public bool RestrictDragItemsToPrimaryPlaceholder(IList<object> dragItems)
    {
        ArgumentNullException.ThrowIfNull(dragItems);
        if (State.YieldPlaceholders.Count == 0)
        {
            return false;
        }

        dragItems.Clear();
        dragItems.Add(State.YieldPlaceholders[0]);
        return true;
    }

    public LauncherBatchRestorePlan? PlanRestore(IReadOnlyList<IconInfo> tiles, bool cancelled)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        if (!State.IsSuppressedFromTiles)
        {
            return null;
        }

        IReadOnlyList<IconInfo> icons = State.OrderDraggedIcons(State.OriginalIndices.Keys);
        int insertIndex = cancelled
            ? -1
            : Math.Clamp(State.ResolveInsertIndex(tiles), 0, tiles.Count);
        int[] originalIndices = icons
            .Select(icon => State.OriginalIndices.GetValueOrDefault(icon, tiles.Count))
            .ToArray();

        return new LauncherBatchRestorePlan(cancelled, icons, originalIndices, insertIndex);
    }

    public void ApplyRestore(IList<IconInfo> tiles, LauncherBatchRestorePlan plan)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        ArgumentNullException.ThrowIfNull(plan);

        RemoveSessionPlaceholders(tiles);
        if (plan.Cancelled)
        {
            for (int i = 0; i < plan.Icons.Count; i++)
            {
                int requestedIndex = i < plan.OriginalIndices.Count
                    ? plan.OriginalIndices[i]
                    : tiles.Count;
                tiles.Insert(Math.Clamp(requestedIndex, 0, tiles.Count), plan.Icons[i]);
            }
        }
        else
        {
            int insertIndex = Math.Clamp(plan.InsertIndex, 0, tiles.Count);
            for (int i = plan.Icons.Count - 1; i >= 0; i--)
            {
                tiles.Insert(insertIndex, plan.Icons[i]);
            }
        }

        State.Reset();
    }

    public List<IconInfo> BuildTargetPreview(
        IEnumerable<IconInfo>? targetIcons,
        IReadOnlyList<IconInfo> draggedIcons,
        int requestedInsertIndex,
        Predicate<IconInfo>? exclude = null)
    {
        ArgumentNullException.ThrowIfNull(draggedIcons);

        var draggedSet = new HashSet<IconInfo>(draggedIcons);
        var tiles = new List<IconInfo>();
        if (targetIcons != null)
        {
            foreach (IconInfo icon in targetIcons)
            {
                if (icon != null
                    && !draggedSet.Contains(icon)
                    && (exclude == null || !exclude(icon)))
                {
                    tiles.Add(icon);
                }
            }
        }

        int insertAt = Math.Clamp(requestedInsertIndex, 0, tiles.Count);
        tiles.InsertRange(insertAt, State.YieldPlaceholders);
        State.InsertIndex = insertAt;
        return tiles;
    }

    public int RemoveSessionPlaceholders(IList<IconInfo> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        int removed = 0;
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (!State.YieldPlaceholders.Contains(tiles[i]))
            {
                continue;
            }

            tiles.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    public static int RemoveOrphanPlaceholders(IList<IconInfo> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        int removed = 0;
        for (int i = tiles.Count - 1; i >= 0; i--)
        {
            if (tiles[i]?.IsDragYieldPlaceholder_NoWrite != true)
            {
                continue;
            }

            tiles.RemoveAt(i);
            removed++;
        }

        return removed;
    }

    private static IconInfo CreatePlaceholder(IconInfo sourceIcon, int groupCount)
    {
        int columnSpan = sourceIcon.IsFolder
            ? Math.Max(1, sourceIcon.FolderColumnSpan_NoWrite)
            : 1;
        int rowSpan = sourceIcon.IsFolder
            ? Math.Max(1, sourceIcon.FolderRowSpan_NoWrite)
            : 1;

        return new IconInfo
        {
            IsDragYieldPlaceholder_NoWrite = true,
            BatchDragGroupCount_NoWrite = Math.Max(1, groupCount),
            DragPlaceholderColumnSpan_NoWrite = columnSpan,
            DragPlaceholderRowSpan_NoWrite = rowSpan
        };
    }
}

internal sealed record LauncherBatchRestorePlan(
    bool Cancelled,
    IReadOnlyList<IconInfo> Icons,
    IReadOnlyList<int> OriginalIndices,
    int InsertIndex);
