using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

public enum LauncherShortcutListUpdateKind
{
    None,
    Incremental,
    Reset
}

/// <summary>
/// Launcher 可见快捷方式列表状态。
/// 在小范围编辑时保留已实现的 UI 容器，切换到大部分不同的数据集时使用单次重置。
/// </summary>
public sealed class LauncherShortcutListState
{
    public BulkObservableCollection<IconInfo> Items { get; } = new();

    public LauncherShortcutListUpdateKind ApplySnapshot(IReadOnlyList<IconInfo> snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (Items.SequenceEqual(snapshot))
        {
            return LauncherShortcutListUpdateKind.None;
        }

        if (Items.Count == 0 || snapshot.Count == 0)
        {
            Items.ReplaceAll(snapshot);
            return LauncherShortcutListUpdateKind.Reset;
        }

        var currentItems = new HashSet<IconInfo>(Items);
        int sharedCount = snapshot.Count(currentItems.Contains);
        if (sharedCount == 0)
        {
            Items.ReplaceAll(snapshot);
            return LauncherShortcutListUpdateKind.Reset;
        }

        // 分组与“全部”之间切换时，通常只有一个小分组与整表重合。
        // 此时逐项 Insert/Move 会产生大量 UI 通知，并让 SynchronizeWith
        // 退化为平方级查找。差异超过一半时直接发送一次 Reset，交给
        // 虚拟化面板仅实现当前可见的容器。
        int largerCount = Math.Max(Items.Count, snapshot.Count);
        if (sharedCount * 2 < largerCount)
        {
            Items.ReplaceAll(snapshot);
            return LauncherShortcutListUpdateKind.Reset;
        }

        Items.SynchronizeWith(snapshot);
        return LauncherShortcutListUpdateKind.Incremental;
    }
}
