using System.Collections.ObjectModel;
using rainspace.Services;
using rainspace.ViewModel;

namespace rainspace.Tests.Services;

[TestClass]
public class ShortcutFolderTreeServiceTests
{
    [TestMethod]
    public void TryFindContainer_FindsTopLevelIcon()
    {
        var icon = new IconInfo { Name = "App" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { icon } };
        var menus = new ObservableCollection<MenuInfo> { menu };

        bool found = ShortcutFolderTreeService.TryFindContainer(menus, icon, out var container);

        Assert.IsTrue(found);
        Assert.AreSame(menu.IconList, container.Collection);
        Assert.IsNull(container.ParentFolder);
        Assert.AreSame(menu, container.Menu);
    }

    [TestMethod]
    public void TryFindContainer_FindsNestedIcon()
    {
        var child = new IconInfo { Name = "Child" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };
        folder.FolderItems.Add(child);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { folder } };
        var menus = new ObservableCollection<MenuInfo> { menu };

        bool found = ShortcutFolderTreeService.TryFindContainer(menus, child, out var container);

        Assert.IsTrue(found);
        Assert.AreSame(folder.FolderItems, container.Collection);
        Assert.AreSame(folder, container.ParentFolder);
        Assert.AreSame(menu, container.Menu);
    }

    [TestMethod]
    public void IsDescendant_ReturnsTrueForNestedFolder()
    {
        var root = new IconInfo { Name = "Root", IsFolder = true };
        var nested = new IconInfo { Name = "Nested", IsFolder = true };
        root.FolderItems.Add(nested);

        Assert.IsTrue(ShortcutFolderTreeService.IsDescendant(root, nested));
        Assert.IsFalse(ShortcutFolderTreeService.IsDescendant(nested, root));
    }

    [TestMethod]
    public void CanMoveIntoFolder_RejectsMovingFolderIntoDescendant()
    {
        var root = new IconInfo { Name = "Root", IsFolder = true };
        var nested = new IconInfo { Name = "Nested", IsFolder = true };
        root.FolderItems.Add(nested);

        Assert.IsFalse(ShortcutFolderTreeService.CanMoveIntoFolder(root, nested));
    }

    [TestMethod]
    public void TryFindContainer_ReturnsFalseForUnknownIcon()
    {
        var icon = new IconInfo { Name = "Ghost" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo>() };
        var menus = new ObservableCollection<MenuInfo> { menu };

        bool found = ShortcutFolderTreeService.TryFindContainer(menus, icon, out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void CanMoveIntoFolder_AllowsNormalShortcutIntoFolder()
    {
        var icon = new IconInfo { Name = "App" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };

        Assert.IsTrue(ShortcutFolderTreeService.CanMoveIntoFolder(icon, folder));
    }

    [TestMethod]
    public void CanMoveIntoFolder_RejectsFolderIntoFolder()
    {
        var childFolder = new IconInfo { Name = "Child", IsFolder = true };
        var parentFolder = new IconInfo { Name = "Parent", IsFolder = true };

        Assert.IsFalse(ShortcutFolderTreeService.CanMoveIntoFolder(childFolder, parentFolder));
    }

    [TestMethod]
    public void CanMoveIntoFolder_RejectsSelfMove()
    {
        var folder = new IconInfo { Name = "Folder", IsFolder = true };

        Assert.IsFalse(ShortcutFolderTreeService.CanMoveIntoFolder(folder, folder));
    }

    [TestMethod]
    public void CanMoveIntoFolder_RejectsPlaceholder()
    {
        var placeholder = new IconInfo { Name = "Placeholder", IsDragYieldPlaceholder_NoWrite = true };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };

        Assert.IsFalse(ShortcutFolderTreeService.CanMoveIntoFolder(placeholder, folder));
    }

    [TestMethod]
    public void CanMoveIntoFolder_RejectsAddShortcut()
    {
        var add = new IconInfo { Name = "Add", IsAddShortcut_NoWrite = true };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };

        Assert.IsFalse(ShortcutFolderTreeService.CanMoveIntoFolder(add, folder));
    }

    [TestMethod]
    public void IsDescendant_ReturnsFalseForDeeplyNested()
    {
        var root = new IconInfo { Name = "Root", IsFolder = true };
        var mid = new IconInfo { Name = "Mid", IsFolder = true };
        var deep = new IconInfo { Name = "Deep" };
        root.FolderItems.Add(mid);
        mid.FolderItems.Add(deep);

        Assert.IsTrue(ShortcutFolderTreeService.IsDescendant(root, deep));
        Assert.IsFalse(ShortcutFolderTreeService.IsDescendant(deep, root));
    }

    [TestMethod]
    public void EnumerateIcons_ReturnsTopLevelAndNestedItems()
    {
        var top = new IconInfo { Name = "Top" };
        var child = new IconInfo { Name = "Child" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };
        folder.FolderItems.Add(child);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { top, folder } };

        var icons = ShortcutFolderTreeService.EnumerateIcons(new[] { menu }).ToList();

        CollectionAssert.AreEqual(new[] { top, folder, child }, icons);
    }

    [TestMethod]
    public void FindGroupContainingIcon_FindsNestedChildGroup()
    {
        var child = new IconInfo { Name = "Child" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };
        folder.FolderItems.Add(child);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { folder } };

        MenuInfo? group = ShortcutFolderTreeService.FindGroupContainingIcon(new[] { menu }, child);

        Assert.AreSame(menu, group);
    }

    [TestMethod]
    public void IsRootLevelItem_ReturnsTrueForTopLevelIcon()
    {
        var icon = new IconInfo { Name = "App" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { icon } };

        Assert.IsTrue(ShortcutFolderTreeService.IsRootLevelItem(new[] { menu }, icon));
    }

    [TestMethod]
    public void IsRootLevelItem_ReturnsFalseForNestedIcon()
    {
        var child = new IconInfo { Name = "Child" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };
        folder.FolderItems.Add(child);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { folder } };

        Assert.IsTrue(ShortcutFolderTreeService.IsRootLevelItem(new[] { menu }, folder));
        Assert.IsFalse(ShortcutFolderTreeService.IsRootLevelItem(new[] { menu }, child));
    }

    [TestMethod]
    public void RemoveFromTree_RemovesNestedChildAndRefreshesFolder()
    {
        var child = new IconInfo { Name = "Child" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true };
        folder.FolderItems.Add(child);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { folder } };

        bool removed = ShortcutFolderTreeService.RemoveFromTree(new[] { menu }, child);

        Assert.IsTrue(removed);
        Assert.AreEqual(0, menu.IconList.Count);
        Assert.AreEqual(0, folder.FolderItems.Count);
    }
}
