using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Presentation;

/// <summary>把 Presentation 模型映射为上下文菜单能力与稳定 ID 命令。</summary>
public sealed class LauncherContextMenuCoordinator<TItem> where TItem : class
{
    private readonly Func<TItem, string?> _getId;
    private readonly Func<TItem, bool> _isFolder;
    private readonly Func<TItem, bool> _isPlaceholder;
    private readonly Func<TItem, bool> _hasFileLocation;

    public LauncherContextMenuCoordinator(
        Func<TItem, string?> getId,
        Func<TItem, bool> isFolder,
        Func<TItem, bool> isPlaceholder,
        Func<TItem, bool> hasFileLocation)
    {
        _getId = getId ?? throw new ArgumentNullException(nameof(getId));
        _isFolder = isFolder ?? throw new ArgumentNullException(nameof(isFolder));
        _isPlaceholder = isPlaceholder ?? throw new ArgumentNullException(nameof(isPlaceholder));
        _hasFileLocation = hasFileLocation ?? throw new ArgumentNullException(nameof(hasFileLocation));
    }

    public LauncherContextMenuPlan Resolve(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return LauncherContextMenuPolicy.Resolve(new LauncherContextMenuRequest(
            _getId(item),
            _isFolder(item),
            _isPlaceholder(item),
            _hasFileLocation(item)));
    }

    public LauncherMutationCommand? CreateRemoveCommand(TItem item) =>
        CanUse(item, LauncherContextMenuAction.Remove)
            ? new LauncherMutationCommand(
                LauncherMutationKind.RemoveShortcut,
                SubjectShortcutId: _getId(item))
            : null;

    public LauncherMutationCommand? CreateResizeFolderCommand(
        TItem item,
        LauncherFolderSize size) =>
        CanUse(item, LauncherContextMenuAction.ResizeFolder)
            ? new LauncherMutationCommand(
                LauncherMutationKind.ResizeFolder,
                SubjectShortcutId: _getId(item),
                FolderSize: size)
            : null;

    public LauncherMutationCommand? CreateMoveOutOfFolderCommand(
        TItem folder,
        TItem child,
        int insertIndex)
    {
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(child);
        string? folderId = _getId(folder);
        string? childId = _getId(child);
        if (!_isFolder(folder)
            || _isPlaceholder(child)
            || string.IsNullOrWhiteSpace(folderId)
            || string.IsNullOrWhiteSpace(childId))
        {
            return null;
        }

        return new LauncherMutationCommand(
            LauncherMutationKind.MoveOutOfFolder,
            TargetShortcutId: folderId,
            InsertIndex: insertIndex,
            SubjectShortcutId: childId);
    }

    public bool CanUse(TItem item, LauncherContextMenuAction action)
    {
        ArgumentNullException.ThrowIfNull(item);
        return Resolve(item).IsVisible(action);
    }
}
