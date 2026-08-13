using System.Collections.ObjectModel;
using rainspace.ViewModel;

namespace rainspace.Services;

public static class AllGroupsShortcutViewService
{
    /// <summary>
    /// JSON stores All Groups folder members as nested objects. Reconnect members
    /// to the canonical MenuList instances by stable ID after load/import so the
    /// same shortcut cannot diverge into two mutable objects.
    /// </summary>
    public static bool NormalizeOwnedFolderReferences(AppData? data)
    {
        if (data?.MenuList == null || data.AppConfig?.AllGroupsIconList == null)
        {
            return false;
        }

        Dictionary<string, IconInfo> canonicalById = ShortcutFolderTreeService
            .EnumerateIcons(data.MenuList)
            .Where(icon => icon != null && !string.IsNullOrWhiteSpace(icon.IconId))
            .GroupBy(icon => icon.IconId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        if (canonicalById.Count == 0)
        {
            return false;
        }

        bool changed = false;
        var visited = new HashSet<IconInfo>();
        foreach (IconInfo root in data.AppConfig.AllGroupsIconList)
        {
            changed |= NormalizeFolderChildren(root, canonicalById, visited);
        }
        return changed;
    }

    private static bool NormalizeFolderChildren(
        IconInfo? folder,
        IReadOnlyDictionary<string, IconInfo> canonicalById,
        ISet<IconInfo> visited)
    {
        if (folder?.IsFolder != true || !visited.Add(folder))
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < folder.FolderItems.Count; i++)
        {
            IconInfo child = folder.FolderItems[i];
            if (!string.IsNullOrWhiteSpace(child.IconId)
                && canonicalById.TryGetValue(child.IconId, out IconInfo? canonical)
                && !ReferenceEquals(child, canonical))
            {
                folder.FolderItems[i] = canonical;
                child = canonical;
                changed = true;
            }

            changed |= NormalizeFolderChildren(child, canonicalById, visited);
        }

        if (changed)
        {
            folder.RefreshFolderMetadata();
        }
        return changed;
    }

    public static List<IconInfo> BuildDefaultFlatOrder(
        IEnumerable<MenuInfo>? menus,
        ObservableCollection<IconInfo>? allGroupsItems)
    {
        var result = new List<IconInfo>();
        var used = new HashSet<IconInfo>();

        foreach (IconInfo item in EnumerateAllGroupsRootItems(allGroupsItems))
        {
            if (item == null || item.IsAddShortcut_NoWrite || !used.Add(item))
            {
                continue;
            }

            result.Add(item);
        }

        foreach (IconInfo icon in EnumerateMenuShortcuts(menus))
        {
            if (icon == null
                || icon.IsFolder
                || icon.IsAddShortcut_NoWrite
                || IsContainedInAllGroupsFolder(allGroupsItems, icon)
                || !used.Add(icon))
            {
                continue;
            }

            result.Add(icon);
        }

        return result;
    }

    public static bool IsVisibleFlatItem(
        IEnumerable<MenuInfo>? menus,
        ObservableCollection<IconInfo>? allGroupsItems,
        IconInfo? icon)
    {
        if (icon == null || icon.IsAddShortcut_NoWrite)
        {
            return false;
        }

        if (IsAllGroupsRootItem(allGroupsItems, icon))
        {
            return true;
        }

        return !icon.IsFolder
            && !IsContainedInAllGroupsFolder(allGroupsItems, icon)
            && ShortcutFolderTreeService.TryFindContainer(menus ?? Array.Empty<MenuInfo>(), icon, out _);
    }

    public static bool IsAllGroupsRootItem(
        ObservableCollection<IconInfo>? allGroupsItems,
        IconInfo? icon)
    {
        return icon != null && allGroupsItems?.Contains(icon) == true;
    }

    public static bool IsContainedInAllGroupsFolder(
        ObservableCollection<IconInfo>? allGroupsItems,
        IconInfo? icon)
    {
        if (icon == null || allGroupsItems == null)
        {
            return false;
        }

        foreach (IconInfo root in allGroupsItems)
        {
            if (root?.IsFolder == true && ShortcutFolderTreeService.IsDescendant(root, icon))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryCreateOwnedFolder(
        ObservableCollection<IconInfo>? allGroupsItems,
        IList<IconInfo>? flatOrder,
        IconInfo? draggedIcon,
        IconInfo? targetIcon,
        int visualInsertIndex,
        out IconInfo? folder)
    {
        folder = null;
        if (allGroupsItems == null
            || flatOrder == null
            || !ShortcutFolderService.CanCreateFolderFrom(draggedIcon, targetIcon))
        {
            return false;
        }

        folder = ShortcutFolderService.CreateFolder();
        folder.FolderItems.Add(targetIcon!);
        folder.FolderItems.Add(draggedIcon!);
        folder.RefreshFolderMetadata();
        allGroupsItems.Add(folder);

        int clampedVisualInsertIndex = Math.Clamp(visualInsertIndex, 0, flatOrder.Count);
        int removedBeforeInsert = 0;
        for (int i = 0; i < clampedVisualInsertIndex; i++)
        {
            IconInfo icon = flatOrder[i];
            if (ReferenceEquals(icon, draggedIcon) || ReferenceEquals(icon, targetIcon))
            {
                removedBeforeInsert++;
            }
        }

        for (int i = flatOrder.Count - 1; i >= 0; i--)
        {
            IconInfo icon = flatOrder[i];
            if (ReferenceEquals(icon, draggedIcon) || ReferenceEquals(icon, targetIcon))
            {
                flatOrder.RemoveAt(i);
            }
        }

        int insertIndex = Math.Clamp(clampedVisualInsertIndex - removedBeforeInsert, 0, flatOrder.Count);
        flatOrder.Insert(insertIndex, folder);
        return true;
    }

    public static bool TryMoveIntoOwnedFolder(
        ObservableCollection<IconInfo>? allGroupsItems,
        IList<IconInfo>? flatOrder,
        IconInfo? draggedIcon,
        IconInfo? targetFolder)
    {
        if (allGroupsItems == null
            || flatOrder == null
            || targetFolder?.IsFolder != true
            || !IsAllGroupsRootItem(allGroupsItems, targetFolder)
            || !ShortcutFolderService.CanMoveIntoFolder(draggedIcon, targetFolder))
        {
            return false;
        }

        if (!targetFolder.FolderItems.Contains(draggedIcon!))
        {
            targetFolder.FolderItems.Add(draggedIcon!);
            targetFolder.RefreshFolderMetadata();
        }

        for (int i = flatOrder.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(flatOrder[i], draggedIcon))
            {
                flatOrder.RemoveAt(i);
            }
        }

        return true;
    }

    public static bool TryMoveOutOfOwnedFolder(
        ObservableCollection<IconInfo>? allGroupsItems,
        IList<IconInfo>? flatOrder,
        IconInfo? sourceFolder,
        IconInfo? child,
        int insertIndex)
    {
        if (allGroupsItems == null
            || flatOrder == null
            || sourceFolder?.IsFolder != true
            || child == null
            || !IsAllGroupsRootItem(allGroupsItems, sourceFolder)
            || !sourceFolder.FolderItems.Remove(child))
        {
            return false;
        }

        int safeInsertIndex = Math.Clamp(insertIndex, 0, flatOrder.Count);
        flatOrder.Remove(child);
        flatOrder.Insert(safeInsertIndex, child);
        sourceFolder.RefreshFolderMetadata();

        NormalizeSparseOwnedFolder(allGroupsItems, flatOrder, sourceFolder);
        return true;
    }

    private static void NormalizeSparseOwnedFolder(
        ObservableCollection<IconInfo> allGroupsItems,
        IList<IconInfo> flatOrder,
        IconInfo folder)
    {
        if (folder.FolderItems.Count > 1)
        {
            return;
        }

        int folderFlatIndex = flatOrder.IndexOf(folder);
        flatOrder.Remove(folder);
        allGroupsItems.Remove(folder);

        if (folder.FolderItems.Count == 0)
        {
            return;
        }

        IconInfo remaining = folder.FolderItems[0];
        folder.FolderItems.RemoveAt(0);
        int insertIndex = folderFlatIndex >= 0
            ? Math.Clamp(folderFlatIndex, 0, flatOrder.Count)
            : 0;
        if (!flatOrder.Contains(remaining))
        {
            flatOrder.Insert(insertIndex, remaining);
        }

        folder.RefreshFolderMetadata();
    }

    public static void ApplyVisibleOrderPreservingHiddenItems(
        ObservableCollection<IconInfo>? iconList,
        IList<IconInfo>? desiredOrder)
    {
        if (iconList == null || desiredOrder == null || desiredOrder.Count == 0)
        {
            return;
        }

        var desiredSet = new HashSet<IconInfo>(desiredOrder.Where(icon => icon != null));
        var visibleSlots = new List<int>();
        for (int i = 0; i < iconList.Count; i++)
        {
            if (desiredSet.Contains(iconList[i]))
            {
                visibleSlots.Add(i);
            }
        }

        if (visibleSlots.Count == 0)
        {
            return;
        }

        var desiredInCollection = desiredOrder
            .Where(icon => icon != null && iconList.Contains(icon))
            .Take(visibleSlots.Count)
            .ToList();
        if (desiredInCollection.Count == 0)
        {
            return;
        }

        var finalOrder = iconList.ToList();
        int replacementCount = Math.Min(visibleSlots.Count, desiredInCollection.Count);
        for (int i = 0; i < replacementCount; i++)
        {
            finalOrder[visibleSlots[i]] = desiredInCollection[i];
        }

        for (int targetIndex = 0; targetIndex < finalOrder.Count; targetIndex++)
        {
            IconInfo icon = finalOrder[targetIndex];
            int currentIndex = iconList.IndexOf(icon);
            if (currentIndex >= 0 && currentIndex != targetIndex)
            {
                iconList.Move(currentIndex, targetIndex);
            }
        }
    }

    private static IEnumerable<IconInfo> EnumerateAllGroupsRootItems(ObservableCollection<IconInfo>? allGroupsItems)
    {
        if (allGroupsItems == null)
        {
            yield break;
        }

        foreach (IconInfo item in allGroupsItems)
        {
            if (item != null)
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<IconInfo> EnumerateMenuShortcuts(IEnumerable<MenuInfo>? menus)
    {
        if (menus == null)
        {
            yield break;
        }

        foreach (IconInfo icon in ShortcutFolderTreeService.EnumerateIcons(menus))
        {
            if (icon != null)
            {
                yield return icon;
            }
        }
    }
}
