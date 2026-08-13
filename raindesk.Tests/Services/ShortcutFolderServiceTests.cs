using System.Collections.ObjectModel;
using raindesk.Constant;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class ShortcutFolderServiceTests
{
    [TestMethod]
    public void CreateFolderFromIcons_ReplacesBothIconsWithFolder()
    {
        var first = new IconInfo { Name = "Word", Path = @"C:\Apps\word.exe" };
        var second = new IconInfo { Name = "Excel", Path = @"C:\Apps\excel.exe" };
        var icons = new ObservableCollection<IconInfo> { first, second };

        IconInfo folder = ShortcutFolderService.CreateFolderFromIcons(icons, first, second);

        Assert.AreEqual(1, icons.Count);
        Assert.AreSame(folder, icons[0]);
        Assert.IsTrue(folder.IsFolder);
        Assert.AreEqual("新建文件夹", folder.Name);
        Assert.AreEqual(ShortcutFolderSize.Medium, folder.FolderSize);
        CollectionAssert.AreEqual(new[] { first, second }, folder.FolderItems.ToArray());
    }

    [TestMethod]
    public void CreateFolderFromIcons_KeepsDropTargetOrder_WhenDraggedIconWasAfterTarget()
    {
        var target = new IconInfo { Name = "Target" };
        var dragged = new IconInfo { Name = "Dragged" };
        var icons = new ObservableCollection<IconInfo> { target, dragged };

        IconInfo folder = ShortcutFolderService.CreateFolderFromIcons(icons, dragged, target);

        Assert.AreSame(folder, icons[0]);
        CollectionAssert.AreEqual(new[] { target, dragged }, folder.FolderItems.ToArray());
    }

    [TestMethod]
    public void MoveIconIntoFolder_RemovesIconFromSourceAndAddsToFolder()
    {
        var icon = new IconInfo { Name = "PowerPoint" };
        var folder = ShortcutFolderService.CreateFolder("Office");
        var icons = new ObservableCollection<IconInfo> { icon, folder };

        bool moved = ShortcutFolderService.MoveIconIntoFolder(icons, icon, folder);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, icons.Count);
        Assert.AreSame(folder, icons[0]);
        CollectionAssert.AreEqual(new[] { icon }, folder.FolderItems.ToArray());
    }

    [TestMethod]
    public void MoveIconIntoFolder_RejectsFolderNesting()
    {
        var folder = ShortcutFolderService.CreateFolder("Folder");
        var target = ShortcutFolderService.CreateFolder("Target");
        var icons = new ObservableCollection<IconInfo> { folder, target };

        bool moved = ShortcutFolderService.MoveIconIntoFolder(icons, folder, target);

        Assert.IsFalse(moved);
        Assert.AreEqual(2, icons.Count);
        Assert.AreEqual(0, target.FolderItems.Count);
    }

    [TestMethod]
    public void MoveIconOutOfFolder_InsertsAfterFolder()
    {
        var icon = new IconInfo { Name = "OneNote" };
        var folder = ShortcutFolderService.CreateFolder("Office");
        folder.FolderItems.Add(icon);
        var tail = new IconInfo { Name = "Tail" };
        var icons = new ObservableCollection<IconInfo> { folder, tail };

        bool moved = ShortcutFolderService.MoveIconOutOfFolder(icons, folder, icon, insertIndex: 1);

        Assert.IsTrue(moved);
        CollectionAssert.AreEqual(new[] { folder, icon, tail }, icons.ToArray());
        Assert.AreEqual(0, folder.FolderItems.Count);
    }

    [TestMethod]
    public void MoveIconOutOfFolder_RemovesEmptyFolder_WhenMenusProvided()
    {
        var icon = new IconInfo { Name = "OneNote" };
        var folder = ShortcutFolderService.CreateFolder("Office");
        folder.FolderItems.Add(icon);
        var tail = new IconInfo { Name = "Tail" };
        var icons = new ObservableCollection<IconInfo> { folder, tail };
        var menus = new List<MenuInfo> { new() { IconList = icons } };

        bool moved = ShortcutFolderService.MoveIconOutOfFolder(icons, folder, icon, insertIndex: 1, menus);

        Assert.IsTrue(moved);
        CollectionAssert.AreEqual(new[] { icon, tail }, icons.ToArray());
        Assert.AreEqual(0, folder.FolderItems.Count);
    }

    [TestMethod]
    public void NormalizeSparseFolders_UnwrapsSingleItemFolder()
    {
        var icon = new IconInfo { Name = "Only" };
        var folder = ShortcutFolderService.CreateFolder("Office");
        folder.FolderItems.Add(icon);
        var icons = new ObservableCollection<IconInfo> { folder };
        var menus = new List<MenuInfo> { new() { IconList = icons } };

        bool changed = ShortcutFolderService.NormalizeSparseFolders(menus, folder);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, icons.Count);
        Assert.AreSame(icon, icons[0]);
        Assert.IsFalse(icons[0].IsFolder);
    }

    [TestMethod]
    public void NormalizeSparseFolders_RemovesEmptyFolder()
    {
        var folder = ShortcutFolderService.CreateFolder("Office");
        var icons = new ObservableCollection<IconInfo> { folder };
        var menus = new List<MenuInfo> { new() { IconList = icons } };

        bool changed = ShortcutFolderService.NormalizeSparseFolders(menus, folder);

        Assert.IsTrue(changed);
        Assert.AreEqual(0, icons.Count);
    }

    [TestMethod]
    public void NormalizeAllSparseFolders_RemovesEmptyFoldersAcrossMenus()
    {
        var emptyA = ShortcutFolderService.CreateFolder("EmptyA");
        var emptyB = ShortcutFolderService.CreateFolder("EmptyB");
        var icon = new IconInfo { Name = "Keep" };
        var menus = new List<MenuInfo>
        {
            new() { MenuName = "A", IconList = new ObservableCollection<IconInfo> { emptyA, icon } },
            new() { MenuName = "B", IconList = new ObservableCollection<IconInfo> { emptyB } }
        };

        bool changed = ShortcutFolderService.NormalizeAllSparseFolders(menus);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, menus[0].IconList!.Count);
        Assert.AreSame(icon, menus[0].IconList[0]);
        Assert.AreEqual(0, menus[1].IconList!.Count);
    }

    [TestMethod]
    public void NormalizeSparseFolders_UnwrapsNestedSingleItemFolders()
    {
        var icon = new IconInfo { Name = "Leaf" };
        var inner = ShortcutFolderService.CreateFolder("Inner");
        inner.FolderItems.Add(icon);
        var outer = ShortcutFolderService.CreateFolder("Outer");
        outer.FolderItems.Add(inner);
        var icons = new ObservableCollection<IconInfo> { outer };
        var menus = new List<MenuInfo> { new() { IconList = icons } };

        bool changed = ShortcutFolderService.NormalizeSparseFolders(menus, inner);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, icons.Count);
        Assert.AreSame(icon, icons[0]);
    }

    [TestMethod]
    public void ChangeFolderSize_UpdatesOnlyFolders()
    {
        var folder = ShortcutFolderService.CreateFolder("Office");
        var normal = new IconInfo { Name = "Normal" };

        Assert.IsTrue(ShortcutFolderService.ChangeFolderSize(folder, ShortcutFolderSize.Large));
        Assert.IsFalse(ShortcutFolderService.ChangeFolderSize(normal, ShortcutFolderSize.Large));
        Assert.AreEqual(ShortcutFolderSize.Large, folder.FolderSize);
        Assert.AreEqual(ShortcutFolderSize.Small, normal.FolderSize);
    }

    [TestMethod]
    public void MoveIconIntoFolder_RejectsFolderIntoFolder()
    {
        var childFolder = ShortcutFolderService.CreateFolder("Child");
        var parentFolder = ShortcutFolderService.CreateFolder("Parent");
        var source = new ObservableCollection<IconInfo> { childFolder, parentFolder };

        bool moved = ShortcutFolderService.MoveItemIntoFolder(source, childFolder, parentFolder);

        Assert.IsFalse(moved);
        Assert.AreEqual(2, source.Count);
        Assert.AreEqual(0, parentFolder.FolderItems.Count);
    }

    [TestMethod]
    public void MoveIconIntoFolder_RejectsMovingFolderIntoItsDescendant()
    {
        var root = ShortcutFolderService.CreateFolder("Root");
        var child = ShortcutFolderService.CreateFolder("Child");
        root.FolderItems.Add(child);
        var source = new ObservableCollection<IconInfo> { root };

        bool moved = ShortcutFolderService.MoveItemIntoFolder(source, root, child);

        Assert.IsFalse(moved);
        Assert.AreEqual(1, source.Count);
        Assert.AreSame(root, source[0]);
        Assert.AreSame(child, root.FolderItems[0]);
    }

    [TestMethod]
    public void MoveItemIntoFolder_UpdatesFolderContent()
    {
        var icon = new IconInfo { Name = "App" };
        var folder = ShortcutFolderService.CreateFolder("Folder");
        var source = new ObservableCollection<IconInfo> { icon, folder };

        ShortcutFolderService.MoveItemIntoFolder(source, icon, folder);

        Assert.IsTrue(folder.Content.Contains("1"));
    }
}
