namespace raindesk.Features.Launcher.Application;

public readonly record struct LauncherShortcutDropTraits(
    bool IsFolder,
    bool IsAddShortcut,
    bool IsDragPlaceholder);

/// <summary>
/// Launcher 文件夹拖放资格规则；不依赖 WinUI、持久化或旧 ViewModel。
/// </summary>
public static class LauncherFolderDropPolicy
{
    public static bool CanCreateFolder(
        LauncherShortcutDropTraits dragged,
        LauncherShortcutDropTraits target,
        bool isSameItem)
    {
        return IsNormalShortcut(dragged)
            && IsNormalShortcut(target)
            && !isSameItem;
    }

    public static bool CanMoveIntoFolder(
        LauncherShortcutDropTraits dragged,
        LauncherShortcutDropTraits target,
        bool isSameItem)
    {
        return !dragged.IsFolder
            && !dragged.IsAddShortcut
            && !dragged.IsDragPlaceholder
            && target.IsFolder
            && !isSameItem;
    }

    private static bool IsNormalShortcut(LauncherShortcutDropTraits shortcut) =>
        !shortcut.IsFolder
        && !shortcut.IsAddShortcut
        && !shortcut.IsDragPlaceholder;
}
