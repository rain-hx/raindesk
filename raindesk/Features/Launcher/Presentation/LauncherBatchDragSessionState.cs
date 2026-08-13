using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// 批量拖放的会话状态；不依赖 WinUI 控件、计时器或持久化。
/// </summary>
internal sealed class LauncherBatchDragSessionState
{
    public bool IsSuppressedFromTiles { get; set; }

    public int InsertIndex { get; set; } = -1;

    public int SourceInsertIndex { get; set; } = -1;

    public int LastCommittedTarget { get; private set; } = -1;

    public long LastMoveTick { get; private set; }

    public Dictionary<IconInfo, int> OriginalIndices { get; } = new();

    public List<IconInfo> YieldPlaceholders { get; } = new();

    public IReadOnlyList<IconInfo> OrderDraggedIcons(IEnumerable<IconInfo> draggedIcons)
    {
        ArgumentNullException.ThrowIfNull(draggedIcons);
        return draggedIcons
            .OrderBy(icon => OriginalIndices.GetValueOrDefault(icon, int.MaxValue))
            .ToArray();
    }

    public int UpdateInsertIndex(IReadOnlyList<IconInfo> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        int minimum = -1;
        for (int i = 0; i < tiles.Count; i++)
        {
            if (!YieldPlaceholders.Contains(tiles[i]))
            {
                continue;
            }

            minimum = minimum < 0 ? i : Math.Min(minimum, i);
        }

        if (minimum >= 0)
        {
            InsertIndex = minimum;
        }

        return InsertIndex;
    }

    public bool ShouldCommitTargetMove(int targetIndex, long nowTick, int throttleMilliseconds)
    {
        if (InsertIndex < 0 || targetIndex == InsertIndex)
        {
            return false;
        }

        if (LastMoveTick > 0
            && nowTick - LastMoveTick < Math.Max(0, throttleMilliseconds))
        {
            return false;
        }

        return LastCommittedTarget < 0
            || Math.Abs(targetIndex - InsertIndex) != 1
            || targetIndex != LastCommittedTarget;
    }

    public void RecordCommittedMove(int previousIndex, int newInsertIndex, long nowTick)
    {
        LastCommittedTarget = previousIndex;
        InsertIndex = newInsertIndex;
        LastMoveTick = nowTick;
    }

    public void ResetMoveThrottle()
    {
        LastCommittedTarget = -1;
        LastMoveTick = 0;
    }

    public int ResolveInsertIndex(IReadOnlyList<IconInfo> tiles)
    {
        ArgumentNullException.ThrowIfNull(tiles);
        if (InsertIndex >= 0)
        {
            return InsertIndex;
        }

        if (YieldPlaceholders.Count == 0)
        {
            return 0;
        }

        int placeholderIndex = IndexOfReference(tiles, YieldPlaceholders[0]);
        return placeholderIndex >= 0 ? placeholderIndex : 0;
    }

    public void Reset()
    {
        IsSuppressedFromTiles = false;
        InsertIndex = -1;
        SourceInsertIndex = -1;
        ResetMoveThrottle();
        OriginalIndices.Clear();
        YieldPlaceholders.Clear();
    }

    private static int IndexOfReference(IReadOnlyList<IconInfo> items, IconInfo target)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (ReferenceEquals(items[i], target))
            {
                return i;
            }
        }

        return -1;
    }
}
