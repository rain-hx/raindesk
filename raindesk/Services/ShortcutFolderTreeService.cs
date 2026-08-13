using System.Collections.ObjectModel;
using raindesk.ViewModel;
using raindesk.Features.Launcher.Application;

namespace raindesk.Services;

public readonly record struct ShortcutFolderContainer(
    MenuInfo Menu,
    IconInfo? ParentFolder,
    ObservableCollection<IconInfo> Collection);

public static class ShortcutFolderTreeService
{
    public static IEnumerable<IconInfo> EnumerateIcons(IEnumerable<MenuInfo>? menus)
    {
        if (menus == null)
        {
            yield break;
        }

        foreach (MenuInfo menu in menus)
        {
            if (menu?.IconList == null)
            {
                continue;
            }

            foreach (IconInfo icon in EnumerateIconsInCollection(menu.IconList))
            {
                yield return icon;
            }
        }
    }

    public static MenuInfo? FindGroupContainingIcon(IEnumerable<MenuInfo>? menus, IconInfo target)
    {
        return menus != null && TryFindContainer(menus, target, out var container)
            ? container.Menu
            : null;
    }

    public static bool RemoveFromTree(IEnumerable<MenuInfo>? menus, IconInfo target)
    {
        if (menus == null || target == null)
        {
            return false;
        }

        if (!TryFindContainer(menus, target, out var container))
        {
            return false;
        }

        bool removed = container.Collection.Remove(target);
        if (removed)
        {
            container.ParentFolder?.RefreshFolderMetadata();
            if (container.ParentFolder != null)
            {
                ShortcutFolderService.NormalizeSparseFolders(menus, container.ParentFolder);
            }
        }

        return removed;
    }

    public static bool TryFindContainer(
        IEnumerable<MenuInfo> menus,
        IconInfo target,
        out ShortcutFolderContainer container)
    {
        foreach (MenuInfo menu in menus)
        {
            if (menu.IconList == null)
            {
                continue;
            }

            if (TryFindContainerInCollection(menu, null, menu.IconList, target, out container))
            {
                return true;
            }
        }

        container = default;
        return false;
    }

    public static bool IsDescendant(IconInfo possibleAncestor, IconInfo possibleDescendant)
    {
        if (possibleAncestor?.IsFolder != true)
        {
            return false;
        }

        foreach (IconInfo child in possibleAncestor.FolderItems)
        {
            if (ReferenceEquals(child, possibleDescendant))
            {
                return true;
            }

            if (child.IsFolder && IsDescendant(child, possibleDescendant))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsRootLevelItem(IEnumerable<MenuInfo>? menus, IconInfo? target)
    {
        if (menus == null || target == null)
        {
            return false;
        }

        return TryFindContainer(menus, target, out ShortcutFolderContainer container)
            && container.ParentFolder == null;
    }

    public static bool CanMoveIntoFolder(IconInfo item, IconInfo targetFolder)
    {
        return item != null
            && targetFolder != null
            && LauncherFolderDropPolicy.CanMoveIntoFolder(
                ToDropTraits(item),
                ToDropTraits(targetFolder),
                ReferenceEquals(item, targetFolder));
    }

    private static LauncherShortcutDropTraits ToDropTraits(IconInfo icon) => new(
        icon.IsFolder,
        icon.IsAddShortcut_NoWrite,
        icon.IsDragYieldPlaceholder_NoWrite);

    private static IEnumerable<IconInfo> EnumerateIconsInCollection(IEnumerable<IconInfo> icons)
    {
        foreach (IconInfo icon in icons)
        {
            if (icon == null)
            {
                continue;
            }

            yield return icon;
            if (icon.IsFolder)
            {
                foreach (IconInfo child in EnumerateIconsInCollection(icon.FolderItems))
                {
                    yield return child;
                }
            }
        }
    }

    private static bool TryFindContainerInCollection(
        MenuInfo menu,
        IconInfo? parentFolder,
        ObservableCollection<IconInfo> collection,
        IconInfo target,
        out ShortcutFolderContainer container)
    {
        foreach (IconInfo icon in collection)
        {
            if (ReferenceEquals(icon, target))
            {
                container = new ShortcutFolderContainer(menu, parentFolder, collection);
                return true;
            }

            if (icon.IsFolder
                && TryFindContainerInCollection(menu, icon, icon.FolderItems, target, out container))
            {
                return true;
            }
        }

        container = default;
        return false;
    }
}
