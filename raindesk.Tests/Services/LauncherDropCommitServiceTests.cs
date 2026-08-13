using System.Collections.ObjectModel;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class LauncherDropCommitServiceTests
{
    [TestMethod]
    public void CommitCreateFolder_ReplacesTwoIconsWithoutDuplicating()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { dragged, target } };
        var data = CreateAppData(menu);

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.CreateFolder(dragged, target));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(1, menu.IconList.Count);
        Assert.IsTrue(menu.IconList[0].IsFolder);
        CollectionAssert.AreEqual(new[] { dragged, target }, menu.IconList[0].FolderItems.ToArray());
    }

    [TestMethod]
    public void CommitCreateFolder_UsesRequestedDropInsertIndex()
    {
        var dragged = new IconInfo { Name = "A" };
        var before = new IconInfo { Name = "B" };
        var target = new IconInfo { Name = "C" };
        var after = new IconInfo { Name = "D" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { dragged, before, target, after } };
        var data = CreateAppData(menu);

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.CreateFolder(dragged, target, insertIndex: 3));

        Assert.IsTrue(result.Committed);
        Assert.AreSame(before, menu.IconList[0]);
        Assert.IsTrue(menu.IconList[1].IsFolder);
        Assert.AreSame(after, menu.IconList[2]);
        CollectionAssert.AreEqual(new[] { dragged, target }, menu.IconList[1].FolderItems.ToArray());
    }

    [TestMethod]
    public void CommitMoveIntoFolder_MovesNestedSourceItemIntoTargetFolderAndRemovesEmptySource()
    {
        var dragged = new IconInfo { Name = "Nested" };
        var sourceFolder = ShortcutFolderService.CreateFolder("Source");
        sourceFolder.FolderItems.Add(dragged);
        var targetFolder = ShortcutFolderService.CreateFolder("Target");
        var menu = new MenuInfo
        {
            IconList = new ObservableCollection<IconInfo> { sourceFolder, targetFolder }
        };
        var data = CreateAppData(menu);

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.MoveIntoFolder(dragged, targetFolder));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(1, menu.IconList.Count);
        Assert.AreSame(dragged, menu.IconList[0]);
        Assert.IsFalse(menu.IconList[0].IsFolder);
    }

    [TestMethod]
    public void CommitMoveToGroup_MovesNestedItemToTargetGroup()
    {
        var dragged = new IconInfo { Name = "Nested" };
        var sourceFolder = ShortcutFolderService.CreateFolder("Source");
        sourceFolder.FolderItems.Add(dragged);
        var sourceMenu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { sourceFolder } };
        var targetMenu = new MenuInfo { IconList = new ObservableCollection<IconInfo>() };
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { sourceMenu, targetMenu }
        };

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.MoveToGroup(new[] { dragged }, targetMenu, insertIndex: 0));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(0, sourceMenu.IconList.Count);
        Assert.AreSame(dragged, targetMenu.IconList[0]);
    }

    [TestMethod]
    public void CommitReorder_MovesTopLevelItemWithinItsContainer()
    {
        var a = new IconInfo { Name = "A" };
        var b = new IconInfo { Name = "B" };
        var c = new IconInfo { Name = "C" };
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { a, b, c } };
        var data = CreateAppData(menu);

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.Reorder(b, insertIndex: 0));

        Assert.IsTrue(result.Committed);
        CollectionAssert.AreEqual(new[] { b, a, c }, menu.IconList.ToArray());
    }

    [TestMethod]
    public void CommitReorder_MovesNestedItemWithinFolder()
    {
        var a = new IconInfo { Name = "A" };
        var b = new IconInfo { Name = "B" };
        var c = new IconInfo { Name = "C" };
        var folder = ShortcutFolderService.CreateFolder("Folder");
        folder.FolderItems.Add(a);
        folder.FolderItems.Add(b);
        folder.FolderItems.Add(c);
        var menu = new MenuInfo { IconList = new ObservableCollection<IconInfo> { folder } };
        var data = CreateAppData(menu);

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.Reorder(c, insertIndex: 1));

        Assert.IsTrue(result.Committed);
        CollectionAssert.AreEqual(new[] { a, c, b }, folder.FolderItems.ToArray());
        Assert.IsTrue(folder.Content.Contains("3 个项目"));
    }

    [TestMethod]
    public void CommitCreateFolder_MergesShortcutsAcrossMenus()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var sourceMenu = new MenuInfo { MenuName = "Source", IconList = new ObservableCollection<IconInfo> { dragged } };
        var targetMenu = new MenuInfo { MenuName = "Target", IconList = new ObservableCollection<IconInfo> { target } };
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { sourceMenu, targetMenu }
        };

        var result = LauncherDropCommitService.Commit(
            data,
            LauncherDropRequest.CreateFolder(dragged, target));

        Assert.IsTrue(result.Committed);
        Assert.AreEqual(0, sourceMenu.IconList.Count);
        Assert.AreEqual(1, targetMenu.IconList.Count);
        Assert.IsTrue(targetMenu.IconList[0].IsFolder);
        CollectionAssert.AreEqual(new[] { target, dragged }, targetMenu.IconList[0].FolderItems.ToArray());
    }

    private static AppData CreateAppData(MenuInfo menu)
    {
        return new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        };
    }
}
