using System.Collections.ObjectModel;
using rainspace.Constant;
using rainspace.Features.Launcher.Application;
using rainspace.ViewModel;

namespace rainspace.Services;

public static class ShortcutFolderService
{
    public static IconInfo CreateFolder(string name = "新建文件夹")
    {
        var folder = new IconInfo
        {
            Name = string.IsNullOrWhiteSpace(name) ? "新建文件夹" : name,
            IsFolder = true,
            FolderSize = ShortcutFolderSize.Medium,
            FolderItems = new ObservableCollection<IconInfo>()
        };
        folder.RefreshFolderMetadata();
        return folder;
    }

    public static IconInfo CreateFolderFromIcons(
        ObservableCollection<IconInfo> source,
        IconInfo draggedIcon,
        IconInfo targetIcon)
    {
        return CreateFolderFromIcons(source, draggedIcon, targetIcon, insertIndex: -1);
    }

    public static IconInfo CreateFolderFromIcons(
        ObservableCollection<IconInfo> source,
        IconInfo draggedIcon,
        IconInfo targetIcon,
        int insertIndex)
    {
        if (!CanCreateFolderFrom(draggedIcon, targetIcon))
        {
            throw new InvalidOperationException("Only two normal shortcuts can create a folder.");
        }

        int draggedIndex = source.IndexOf(draggedIcon);
        int targetIndex = source.IndexOf(targetIcon);
        if (draggedIndex < 0 || targetIndex < 0)
        {
            throw new InvalidOperationException("Both shortcuts must be in the same source collection.");
        }

        int resolvedInsertIndex = insertIndex >= 0
            ? ResolveInsertionIndexAfterRemoving(source.Count, insertIndex, draggedIndex, targetIndex)
            : Math.Min(draggedIndex, targetIndex);
        var ordered = draggedIndex < targetIndex
            ? new[] { draggedIcon, targetIcon }
            : new[] { targetIcon, draggedIcon };

        RemoveIfPresent(source, draggedIcon);
        RemoveIfPresent(source, targetIcon);

        var folder = CreateFolder();
        foreach (IconInfo icon in ordered)
        {
            folder.FolderItems.Add(icon);
        }

        folder.RefreshFolderMetadata();
        source.Insert(Math.Clamp(resolvedInsertIndex, 0, source.Count), folder);
        return folder;
    }

    public static IconInfo CreateFolderFromIconsAcrossContainers(
        ShortcutFolderContainer draggedContainer,
        ShortcutFolderContainer targetContainer,
        IconInfo draggedIcon,
        IconInfo targetIcon,
        int insertIndex = -1)
    {
        if (!CanCreateFolderFrom(draggedIcon, targetIcon))
        {
            throw new InvalidOperationException("Only two normal shortcuts can create a folder.");
        }

        if (ReferenceEquals(draggedContainer.Collection, targetContainer.Collection))
        {
            return CreateFolderFromIcons(draggedContainer.Collection, draggedIcon, targetIcon, insertIndex);
        }

        int targetIndex = targetContainer.Collection.IndexOf(targetIcon);
        if (targetIndex < 0)
        {
            throw new InvalidOperationException("Target shortcut must exist in its collection.");
        }

        int resolvedInsertIndex = insertIndex >= 0
            ? ResolveInsertionIndexAfterRemoving(targetContainer.Collection.Count, insertIndex, targetIndex)
            : targetIndex;
        RemoveIfPresent(draggedContainer.Collection, draggedIcon);
        RemoveIfPresent(targetContainer.Collection, targetIcon);

        var folder = CreateFolder();
        folder.FolderItems.Add(targetIcon);
        folder.FolderItems.Add(draggedIcon);
        folder.RefreshFolderMetadata();
        targetContainer.Collection.Insert(Math.Clamp(resolvedInsertIndex, 0, targetContainer.Collection.Count), folder);

        draggedContainer.ParentFolder?.RefreshFolderMetadata();
        targetContainer.ParentFolder?.RefreshFolderMetadata();
        return folder;
    }

    public static bool MoveIconIntoFolder(
        ObservableCollection<IconInfo> source,
        IconInfo icon,
        IconInfo targetFolder)
    {
        return MoveItemIntoFolder(source, icon, targetFolder);
    }

    public static bool MoveIconOutOfFolder(
        ObservableCollection<IconInfo> destination,
        IconInfo folder,
        IconInfo icon,
        int insertIndex,
        IEnumerable<MenuInfo>? menus = null)
    {
        if (folder == null || !folder.IsFolder || icon == null)
        {
            return false;
        }

        if (!folder.FolderItems.Remove(icon))
        {
            return false;
        }

        destination.Insert(Math.Clamp(insertIndex, 0, destination.Count), icon);
        folder.RefreshFolderMetadata();

        if (menus != null)
        {
            NormalizeSparseFolders(menus, folder);
        }

        return true;
    }

    /// <summary>
    /// Removes folders with no items and unwraps folders that contain exactly one shortcut.
    /// Walks upward while parent folders also become sparse.
    /// </summary>
    public static bool NormalizeSparseFolders(IEnumerable<MenuInfo>? menus, IconInfo? folder)
    {
        if (menus == null || folder?.IsFolder != true)
        {
            return false;
        }

        bool changed = false;
        IconInfo? current = folder;

        while (current?.IsFolder == true)
        {
            if (!ShortcutFolderTreeService.TryFindContainer(menus, current, out ShortcutFolderContainer container))
            {
                break;
            }

            ObservableCollection<IconInfo> parentCollection = container.Collection;
            int itemCount = current.FolderItems.Count;
            if (itemCount > 1)
            {
                break;
            }

            int folderIndex = parentCollection.IndexOf(current);
            if (folderIndex < 0)
            {
                break;
            }

            changed = true;

            if (itemCount == 0)
            {
                parentCollection.RemoveAt(folderIndex);
                current = container.ParentFolder;
                continue;
            }

            IconInfo child = current.FolderItems[0];
            current.FolderItems.Clear();
            parentCollection[folderIndex] = child;
            current = container.ParentFolder;
        }

        return changed;
    }

    /// <summary>
    /// Scans every menu and removes or unwraps all sparse folders in the tree.
    /// </summary>
    public static bool NormalizeAllSparseFolders(IEnumerable<MenuInfo>? menus)
    {
        if (menus == null)
        {
            return false;
        }

        bool anyChanged = false;
        bool changedInPass;
        int guard = 0;
        do
        {
            changedInPass = false;
            List<IconInfo> folders = CollectFoldersDeepFirst(menus).ToList();
            foreach (IconInfo folder in folders)
            {
                if (folder.FolderItems.Count > 1)
                {
                    continue;
                }

                if (NormalizeSparseFolders(menus, folder))
                {
                    changedInPass = true;
                }
            }

            anyChanged |= changedInPass;
            guard++;
        }
        while (changedInPass && guard < 32);

        return anyChanged;
    }

    private static IEnumerable<IconInfo> CollectFoldersDeepFirst(IEnumerable<MenuInfo> menus)
    {
        foreach (MenuInfo menu in menus)
        {
            if (menu?.IconList == null)
            {
                continue;
            }

            foreach (IconInfo folder in CollectFoldersDeepFirst(menu.IconList))
            {
                yield return folder;
            }
        }
    }

    private static IEnumerable<IconInfo> CollectFoldersDeepFirst(IEnumerable<IconInfo> icons)
    {
        foreach (IconInfo icon in icons)
        {
            if (icon?.IsFolder != true)
            {
                continue;
            }

            foreach (IconInfo nested in CollectFoldersDeepFirst(icon.FolderItems))
            {
                yield return nested;
            }

            yield return icon;
        }
    }

    public static bool ChangeFolderSize(IconInfo icon, ShortcutFolderSize size)
    {
        if (icon == null || !icon.IsFolder)
        {
            return false;
        }

        icon.FolderSize = size;
        return true;
    }

    public static bool CanCreateFolderFrom(IconInfo? draggedIcon, IconInfo? targetIcon)
    {
        return draggedIcon != null
            && targetIcon != null
            && LauncherFolderDropPolicy.CanCreateFolder(
                ToDropTraits(draggedIcon),
                ToDropTraits(targetIcon),
                ReferenceEquals(draggedIcon, targetIcon));
    }

    public static bool CanMoveIntoFolder(IconInfo? icon, IconInfo? targetFolder)
    {
        return icon != null
            && targetFolder != null
            && ShortcutFolderTreeService.CanMoveIntoFolder(icon, targetFolder);
    }

    public static bool MoveItemIntoFolder(
        ObservableCollection<IconInfo> source,
        IconInfo item,
        IconInfo targetFolder,
        IEnumerable<MenuInfo>? menus = null)
    {
        if (!ShortcutFolderTreeService.CanMoveIntoFolder(item, targetFolder))
        {
            return false;
        }

        if (!source.Contains(item))
        {
            return false;
        }

        IconInfo? sourceParentFolder = null;
        if (menus != null
            && ShortcutFolderTreeService.TryFindContainer(menus, item, out ShortcutFolderContainer itemContainer))
        {
            sourceParentFolder = itemContainer.ParentFolder;
        }

        source.Remove(item);
        if (!targetFolder.FolderItems.Contains(item))
        {
            targetFolder.FolderItems.Add(item);
        }

        targetFolder.RefreshFolderMetadata();

        if (menus != null && sourceParentFolder != null)
        {
            NormalizeSparseFolders(menus, sourceParentFolder);
        }

        return true;
    }

    public static IReadOnlyList<IconInfo> GetPreviewItems(IconInfo folder, int maxCount)
    {
        if (folder == null || !folder.IsFolder || maxCount <= 0)
        {
            return Array.Empty<IconInfo>();
        }

        return folder.FolderItems.Take(maxCount).ToList();
    }

    private static LauncherShortcutDropTraits ToDropTraits(IconInfo icon) => new(
        icon.IsFolder,
        icon.IsAddShortcut_NoWrite,
        icon.IsDragYieldPlaceholder_NoWrite);

    private static void RemoveIfPresent(ObservableCollection<IconInfo> source, IconInfo icon)
    {
        if (source.Contains(icon))
        {
            source.Remove(icon);
        }
    }

    private static int ResolveInsertionIndexAfterRemoving(
        int sourceCount,
        int requestedInsertIndex,
        params int[] removedIndices)
    {
        int clampedInsertIndex = Math.Clamp(requestedInsertIndex, 0, sourceCount);
        int removedBeforeInsert = removedIndices
            .Distinct()
            .Count(index => index >= 0 && index < clampedInsertIndex);

        return clampedInsertIndex - removedBeforeInsert;
    }
}
