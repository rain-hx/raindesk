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
        int smallerCount = Math.Min(Items.Count, snapshot.Count);

        if (sharedCount * 2 < smallerCount)
        {
            Items.ReplaceAll(snapshot);
            return LauncherShortcutListUpdateKind.Reset;
        }

        Items.SynchronizeWith(snapshot);
        return LauncherShortcutListUpdateKind.Incremental;
    }
}
