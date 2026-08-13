using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class AllGroupsShortcutViewServiceTests
{
    [TestMethod]
    public void NormalizeOwnedFolderReferences_RelinksJsonCopiesToCanonicalMenuItems()
    {
        var canonical = new IconInfo { IconId = "shared", Name = "Canonical" };
        var outside = new IconInfo { IconId = "outside", Name = "Outside" };
        var deserializedCopy = new IconInfo { IconId = "shared", Name = "Stale Copy" };
        IconInfo ownedFolder = ShortcutFolderService.CreateFolder("Owned");
        ownedFolder.IconId = "folder";
        ownedFolder.FolderItems.Add(deserializedCopy);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                new()
                {
                    MenuId = "group",
                    IconList = new ObservableCollection<IconInfo> { canonical, outside }
                }
            }
        };
        data.AppConfig.AllGroupsIconList.Add(ownedFolder);

        bool changed = AllGroupsShortcutViewService.NormalizeOwnedFolderReferences(data);
        List<IconInfo> flat = AllGroupsShortcutViewService.BuildDefaultFlatOrder(
            data.MenuList,
            data.AppConfig.AllGroupsIconList);

        Assert.IsTrue(changed);
        Assert.AreSame(canonical, ownedFolder.FolderItems.Single());
        CollectionAssert.AreEqual(new[] { ownedFolder, outside }, flat);
    }

    [TestMethod]
    public void BuildDefaultFlatOrder_KeepsAllGroupsFoldersAndExcludesSingleGroupFolders()
    {
        var groupedApp = new IconInfo { Name = "Grouped App" };
        var singleGroupFolder = ShortcutFolderService.CreateFolder("单独分组文件夹");
        singleGroupFolder.FolderItems.Add(groupedApp);

        var looseApp = new IconInfo { Name = "Loose App" };
        var menu = new MenuInfo
        {
            IconList = new ObservableCollection<IconInfo>
            {
                singleGroupFolder,
                looseApp
            }
        };

        var allGroupsFolderChild = new IconInfo { Name = "All Folder Child" };
        var allGroupsFolder = ShortcutFolderService.CreateFolder("全部文件夹");
        allGroupsFolder.FolderItems.Add(allGroupsFolderChild);

        var allGroupsItems = new ObservableCollection<IconInfo> { allGroupsFolder };

        List<IconInfo> flat = AllGroupsShortcutViewService.BuildDefaultFlatOrder(
            new[] { menu },
            allGroupsItems);

        CollectionAssert.Contains(flat, allGroupsFolder);
        CollectionAssert.Contains(flat, groupedApp);
        CollectionAssert.Contains(flat, looseApp);
        CollectionAssert.DoesNotContain(flat, singleGroupFolder);
        CollectionAssert.DoesNotContain(flat, allGroupsFolderChild);
    }

    [TestMethod]
    public void TryCreateOwnedFolder_CreatesFolderWithoutRemovingSourceItems()
    {
        var dragged = new IconInfo { Name = "Dragged" };
        var target = new IconInfo { Name = "Target" };
        var allGroupsItems = new ObservableCollection<IconInfo>();
        var flat = new List<IconInfo> { target, dragged };

        bool created = AllGroupsShortcutViewService.TryCreateOwnedFolder(
            allGroupsItems,
            flat,
            dragged,
            target,
            visualInsertIndex: 1,
            out IconInfo? folder);

        Assert.IsTrue(created);
        Assert.IsNotNull(folder);
        Assert.IsTrue(folder.IsFolder);
        CollectionAssert.Contains(allGroupsItems, folder);
        CollectionAssert.Contains(folder.FolderItems, target);
        CollectionAssert.Contains(folder.FolderItems, dragged);
        CollectionAssert.DoesNotContain(flat, target);
        CollectionAssert.DoesNotContain(flat, dragged);
        Assert.AreSame(folder, flat[0]);
    }

    [TestMethod]
    public void ApplyVisibleOrderPreservingHiddenItems_DoesNotRemoveItemsHiddenByAllGroupsFolder()
    {
        var dragged = new IconInfo { Name = "Dragged" };
        var target = new IconInfo { Name = "Target" };
        var loose = new IconInfo { Name = "Loose" };
        var groupIcons = new ObservableCollection<IconInfo> { dragged, target, loose };

        AllGroupsShortcutViewService.ApplyVisibleOrderPreservingHiddenItems(
            groupIcons,
            new[] { loose });

        CollectionAssert.AreEqual(
            new[] { dragged, target, loose },
            groupIcons.ToList(),
            "Apps hidden inside an all-groups-owned folder must remain in their original shortcut group.");
    }

    [TestMethod]
    public void ApplyVisibleOrderPreservingHiddenItems_ReordersVisibleItemsWithoutMovingHiddenSlots()
    {
        var first = new IconInfo { Name = "First" };
        var hidden = new IconInfo { Name = "Hidden" };
        var second = new IconInfo { Name = "Second" };
        var groupIcons = new ObservableCollection<IconInfo> { first, hidden, second };

        AllGroupsShortcutViewService.ApplyVisibleOrderPreservingHiddenItems(
            groupIcons,
            new[] { second, first });

        CollectionAssert.AreEqual(
            new[] { second, hidden, first },
            groupIcons.ToList(),
            "Only visible all-groups tiles should be reordered; omitted items keep their source-group slot.");
    }

    [TestMethod]
    public void TryMoveIntoOwnedFolder_AddsItemAndRemovesItFromFlatOrder()
    {
        var dragged = new IconInfo { Name = "Dragged" };
        var targetFolder = ShortcutFolderService.CreateFolder("全部文件夹");
        var allGroupsItems = new ObservableCollection<IconInfo> { targetFolder };
        var flat = new List<IconInfo> { targetFolder, dragged };

        bool moved = AllGroupsShortcutViewService.TryMoveIntoOwnedFolder(
            allGroupsItems,
            flat,
            dragged,
            targetFolder);

        Assert.IsTrue(moved);
        CollectionAssert.Contains(targetFolder.FolderItems, dragged);
        CollectionAssert.DoesNotContain(flat, dragged);
        CollectionAssert.Contains(flat, targetFolder);
    }

    [TestMethod]
    public void TryMoveOutOfOwnedFolder_RemovesChildAndInsertsItAfterFolder()
    {
        var first = new IconInfo { Name = "First" };
        var dragged = new IconInfo { Name = "Dragged" };
        var targetFolder = ShortcutFolderService.CreateFolder("全部文件夹");
        targetFolder.FolderItems.Add(first);
        targetFolder.FolderItems.Add(dragged);
        targetFolder.FolderItems.Add(new IconInfo { Name = "Third" });
        var allGroupsItems = new ObservableCollection<IconInfo> { targetFolder };
        var flat = new List<IconInfo> { targetFolder };

        bool moved = AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder(
            allGroupsItems,
            flat,
            targetFolder,
            dragged,
            insertIndex: 1);

        Assert.IsTrue(moved);
        CollectionAssert.DoesNotContain(targetFolder.FolderItems, dragged);
        Assert.AreSame(dragged, flat[1]);
        CollectionAssert.Contains(allGroupsItems, targetFolder);
    }

    [TestMethod]
    public void TryMoveOutOfOwnedFolder_UnwrapsSparseFolder()
    {
        var remaining = new IconInfo { Name = "Remaining" };
        var dragged = new IconInfo { Name = "Dragged" };
        var targetFolder = ShortcutFolderService.CreateFolder("全部文件夹");
        targetFolder.FolderItems.Add(remaining);
        targetFolder.FolderItems.Add(dragged);
        var allGroupsItems = new ObservableCollection<IconInfo> { targetFolder };
        var flat = new List<IconInfo> { targetFolder };

        bool moved = AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder(
            allGroupsItems,
            flat,
            targetFolder,
            dragged,
            insertIndex: 1);

        Assert.IsTrue(moved);
        CollectionAssert.DoesNotContain(allGroupsItems, targetFolder);
        CollectionAssert.AreEqual(new[] { remaining, dragged }, flat);
    }
}
