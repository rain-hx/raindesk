using System.Collections.ObjectModel;
using raindesk.ViewModel;

namespace raindesk.Services;

public enum LauncherDropRequestKind
{
    CreateFolder = 0,
    MoveIntoFolder = 1,
    MoveToGroup = 2,
    Reorder = 3
}

public sealed record LauncherDropRequest(
    LauncherDropRequestKind Kind,
    IconInfo? DraggedIcon,
    IconInfo? TargetIcon,
    IReadOnlyList<IconInfo> Icons,
    MenuInfo? TargetMenu,
    int InsertIndex)
{
    public static LauncherDropRequest CreateFolder(IconInfo draggedIcon, IconInfo targetIcon, int insertIndex = -1)
        => new(
            LauncherDropRequestKind.CreateFolder,
            draggedIcon,
            targetIcon,
            Array.Empty<IconInfo>(),
            null,
            insertIndex);

    public static LauncherDropRequest MoveIntoFolder(IconInfo draggedIcon, IconInfo targetFolder)
        => new(
            LauncherDropRequestKind.MoveIntoFolder,
            draggedIcon,
            targetFolder,
            Array.Empty<IconInfo>(),
            null,
            -1);

    public static LauncherDropRequest MoveToGroup(
        IReadOnlyList<IconInfo> icons,
        MenuInfo targetMenu,
        int insertIndex)
        => new(
            LauncherDropRequestKind.MoveToGroup,
            null,
            null,
            icons,
            targetMenu,
            insertIndex);

    public static LauncherDropRequest Reorder(IconInfo draggedIcon, int insertIndex)
        => new(
            LauncherDropRequestKind.Reorder,
            draggedIcon,
            null,
            Array.Empty<IconInfo>(),
            null,
            insertIndex);
}

public readonly record struct LauncherDropCommitResult(bool Committed);

public static class LauncherDropCommitService
{
    public static LauncherDropCommitResult Commit(AppData? data, LauncherDropRequest request)
    {
        if (data?.MenuList == null)
        {
            return new LauncherDropCommitResult(false);
        }

        bool committed = request.Kind switch
        {
            LauncherDropRequestKind.CreateFolder => CommitCreateFolder(data, request),
            LauncherDropRequestKind.MoveIntoFolder => CommitMoveIntoFolder(data, request),
            LauncherDropRequestKind.MoveToGroup => CommitMoveToGroup(data, request),
            LauncherDropRequestKind.Reorder => CommitReorder(data, request),
            _ => false
        };

        if (committed)
        {
            ShortcutFolderService.NormalizeAllSparseFolders(data.MenuList);
        }

        return new LauncherDropCommitResult(committed);
    }

    private static bool CommitCreateFolder(AppData data, LauncherDropRequest request)
    {
        if (request.DraggedIcon == null || request.TargetIcon == null)
        {
            return false;
        }

        bool foundDragged = ShortcutFolderTreeService.TryFindContainer(data.MenuList, request.DraggedIcon, out var draggedContainer);
        bool foundTarget = ShortcutFolderTreeService.TryFindContainer(data.MenuList, request.TargetIcon, out var targetContainer);
        if (!foundDragged || !foundTarget)
        {
            return false;
        }

        ShortcutFolderService.CreateFolderFromIconsAcrossContainers(
            draggedContainer,
            targetContainer,
            request.DraggedIcon,
            request.TargetIcon,
            request.InsertIndex);
        draggedContainer.ParentFolder?.RefreshFolderMetadata();
        targetContainer.ParentFolder?.RefreshFolderMetadata();
        if (draggedContainer.ParentFolder != null)
        {
            ShortcutFolderService.NormalizeSparseFolders(data.MenuList, draggedContainer.ParentFolder);
        }

        if (targetContainer.ParentFolder != null
            && !ReferenceEquals(targetContainer.ParentFolder, draggedContainer.ParentFolder))
        {
            ShortcutFolderService.NormalizeSparseFolders(data.MenuList, targetContainer.ParentFolder);
        }

        return true;
    }

    private static bool CommitMoveIntoFolder(AppData data, LauncherDropRequest request)
    {
        if (request.DraggedIcon == null || request.TargetIcon?.IsFolder != true)
        {
            return false;
        }

        if (!ShortcutFolderTreeService.TryFindContainer(data.MenuList, request.DraggedIcon, out var sourceContainer))
        {
            return false;
        }

        IconInfo? sourceParentFolder = sourceContainer.ParentFolder;
        bool moved = ShortcutFolderService.MoveItemIntoFolder(
            sourceContainer.Collection,
            request.DraggedIcon,
            request.TargetIcon,
            data.MenuList);
        if (moved)
        {
            sourceParentFolder?.RefreshFolderMetadata();
            request.TargetIcon.RefreshFolderMetadata();
        }

        return moved;
    }

    private static bool CommitMoveToGroup(AppData data, LauncherDropRequest request)
    {
        if (request.TargetMenu?.IconList == null || request.TargetMenu.IsEncrypt)
        {
            return false;
        }

        int insertIndex = Math.Clamp(request.InsertIndex, 0, request.TargetMenu.IconList.Count);
        bool moved = false;
        foreach (IconInfo icon in request.Icons.Where(icon => icon != null).ToList())
        {
            if (!ShortcutFolderTreeService.TryFindContainer(data.MenuList, icon, out var sourceContainer))
            {
                continue;
            }

            ObservableCollection<IconInfo> source = sourceContainer.Collection;
            if (ReferenceEquals(source, request.TargetMenu.IconList))
            {
                int currentIndex = source.IndexOf(icon);
                if (currentIndex >= 0)
                {
                    source.Move(currentIndex, Math.Clamp(insertIndex, 0, source.Count - 1));
                    insertIndex++;
                    moved = true;
                }
                continue;
            }

            IconInfo? sourceParentFolder = sourceContainer.ParentFolder;
            if (!source.Remove(icon))
            {
                continue;
            }

            sourceParentFolder?.RefreshFolderMetadata();
            if (sourceParentFolder != null)
            {
                ShortcutFolderService.NormalizeSparseFolders(data.MenuList, sourceParentFolder);
            }

            insertIndex = Math.Clamp(insertIndex, 0, request.TargetMenu.IconList.Count);
            request.TargetMenu.IconList.Insert(insertIndex, icon);
            insertIndex++;
            moved = true;
        }

        return moved;
    }

    private static bool CommitReorder(AppData data, LauncherDropRequest request)
    {
        if (request.DraggedIcon == null
            || !ShortcutFolderTreeService.TryFindContainer(data.MenuList, request.DraggedIcon, out var sourceContainer))
        {
            return false;
        }

        ObservableCollection<IconInfo> source = sourceContainer.Collection;
        int currentIndex = source.IndexOf(request.DraggedIcon);
        if (currentIndex < 0)
        {
            return false;
        }

        int targetIndex = Math.Clamp(request.InsertIndex, 0, Math.Max(0, source.Count - 1));
        if (targetIndex == currentIndex)
        {
            return false;
        }

        source.Move(currentIndex, targetIndex);
        sourceContainer.ParentFolder?.RefreshFolderMetadata();
        return true;
    }
}
