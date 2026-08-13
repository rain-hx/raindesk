using System.Collections.ObjectModel;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Infrastructure;
using raindesk.Constant;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class AppDataLauncherCommandHandlerTests
{
    [TestMethod]
    public void MoveToGroup_ResolvesStableIdsAndRequestsPersistence()
    {
        var shortcut = new IconInfo { IconId = "shortcut", Name = "Editor" };
        var source = Group("source", shortcut);
        var target = Group("target");
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { source, target }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.MoveToGroup,
            ShortcutIds: new[] { "shortcut" },
            TargetGroupId: "target",
            InsertIndex: 0));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(0, source.IconList.Count);
        Assert.AreSame(shortcut, target.IconList.Single());
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void MissingShortcutId_IsRejectedWithoutMutationOrSave()
    {
        var shortcut = new IconInfo { IconId = "shortcut", Name = "Editor" };
        var source = Group("source", shortcut);
        var target = Group("target");
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { source, target }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.MoveToGroup,
            ShortcutIds: new[] { "missing" },
            TargetGroupId: "target"));

        Assert.IsFalse(result.Success);
        Assert.AreSame(shortcut, source.IconList.Single());
        Assert.AreEqual(0, target.IconList.Count);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void Reorder_UsesShortcutIdAndSavesOnlyWhenChanged()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var menu = Group("group", first, second);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.Reorder,
            DraggedShortcutId: "second",
            InsertIndex: 0));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { second, first }, menu.IconList.ToArray());
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void Add_CreatesStableShortcutInTargetGroupAndClonesImage()
    {
        var menu = Group("group");
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);
        byte[] image = { 1, 2, 3 };

        LauncherCommandResult result = handler.Add(new AddLauncherShortcutCommand(
            "group",
            new LauncherShortcutDraft("Settings", StartArgument: "ms-settings:", ImageBytes: image)));
        image[0] = 99;

        IconInfo added = menu.IconList.Single();
        Assert.IsTrue(result.Success && result.Changed);
        Assert.IsFalse(string.IsNullOrWhiteSpace(added.IconId));
        Assert.AreEqual("Settings", added.Name);
        Assert.AreEqual("ms-settings:", added.StartArg);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, added.ImageByteArr);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void Add_MapsUrlKindWithoutLeakingModelEnumIntoContract()
    {
        var menu = Group("group");
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Add(new AddLauncherShortcutCommand(
            "group",
            new LauncherShortcutDraft(
                "Docs",
                Path: "https://example.test",
                Kind: LauncherShortcutKind.Url)));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(IconType.URL, menu.IconList.Single().IconType);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void AddMany_UsesOneWriteAndSaveWhilePreservingConfiguredGlobalSortBehavior()
    {
        var placeholder = new IconInfo
        {
            IconId = "add",
            Name = "Add",
            IsAddShortcut_NoWrite = true
        };
        var target = Group(
            "target",
            new IconInfo { IconId = "z", Name = "Zulu" },
            placeholder);
        var other = Group(
            "other",
            new IconInfo { IconId = "b", Name = "Beta" },
            new IconInfo { IconId = "a", Name = "Alpha" });
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { target, other }
        };
        data.AppConfig.IconSortType = SortType.NAME_UP;
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.AddMany(new AddLauncherShortcutsCommand(
            "target",
            new[]
            {
                new LauncherShortcutDraft("Charlie"),
                new LauncherShortcutDraft("Bravo")
            },
            ApplyConfiguredSort: true));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(
            new[] { "Bravo", "Charlie", "Zulu", "Add" },
            target.IconList.Select(icon => icon.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "Alpha", "Beta" },
            other.IconList.Select(icon => icon.Name).ToArray());
        Assert.AreSame(placeholder, target.IconList[^1]);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void Rename_UsesStableIdAndDoesNotSaveUnchangedName()
    {
        var shortcut = new IconInfo { IconId = "shortcut", Name = "Editor" };
        var menu = Group("group", shortcut);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult changed = handler.Rename(
            new RenameLauncherShortcutCommand("shortcut", "  IDE  "));
        LauncherCommandResult unchanged = handler.Rename(
            new RenameLauncherShortcutCommand("shortcut", "IDE"));

        Assert.IsTrue(changed.Success && changed.Changed);
        Assert.IsTrue(unchanged.Success && !unchanged.Changed);
        Assert.AreEqual("IDE", shortcut.Name);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void Update_AppliesPropertiesAndClonesImageInSingleSaveBoundary()
    {
        var shortcut = new IconInfo
        {
            IconId = "shortcut",
            Name = "Editor",
            Path = "old",
            StartArg = "--old",
            ImageByteArr = new byte[] { 1 }
        };
        var menu = Group("group", shortcut);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);
        byte[] replacement = { 8, 9 };

        LauncherCommandResult result = handler.Update(new UpdateLauncherShortcutCommand(
            "shortcut",
            new LauncherShortcutPropertiesUpdate(
                "IDE",
                "new",
                "--safe",
                AdminStartUp: true,
                replacement,
                ReplaceImage: true)));
        replacement[0] = 0;

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual("IDE", shortcut.Name);
        Assert.AreEqual("new", shortcut.Path);
        Assert.AreEqual("--safe", shortcut.StartArg);
        Assert.IsTrue(shortcut.AdminStartUp);
        CollectionAssert.AreEqual(new byte[] { 8, 9 }, shortcut.ImageByteArr);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void RemoveShortcut_UsesStableIdNormalizesParentFolderAndSaves()
    {
        var removed = new IconInfo { IconId = "removed", Name = "Removed" };
        var remaining = new IconInfo { IconId = "remaining", Name = "Remaining" };
        IconInfo folder = ShortcutFolderService.CreateFolder("Tools");
        folder.IconId = "folder";
        folder.FolderItems.Add(removed);
        folder.FolderItems.Add(remaining);
        var menu = Group("group", folder);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.RemoveShortcut,
            SubjectShortcutId: "removed"));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreSame(remaining, menu.IconList.Single());
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void ResizeFolder_SavesOnlyWhenSizeActuallyChanges()
    {
        IconInfo folder = ShortcutFolderService.CreateFolder("Tools");
        folder.IconId = "folder";
        folder.FolderSize = ShortcutFolderSize.Small;
        var menu = Group("group", folder);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);
        var command = new LauncherMutationCommand(
            LauncherMutationKind.ResizeFolder,
            SubjectShortcutId: "folder",
            FolderSize: LauncherFolderSize.Medium);

        LauncherCommandResult changed = handler.Execute(command);
        LauncherCommandResult unchanged = handler.Execute(command);

        Assert.IsTrue(changed.Success && changed.Changed);
        Assert.IsTrue(unchanged.Success && !unchanged.Changed);
        Assert.AreEqual(ShortcutFolderSize.Medium, folder.FolderSize);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void ResizeFolder_UpdatesAllGroupsOwnedFolder()
    {
        IconInfo folder = ShortcutFolderService.CreateFolder("Owned");
        folder.IconId = "all-groups-folder";
        folder.FolderSize = ShortcutFolderSize.Medium;
        var data = new AppData();
        data.AppConfig.AllGroupsIconList.Add(folder);
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.ResizeFolder,
            SubjectShortcutId: folder.IconId,
            FolderSize: LauncherFolderSize.Large));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(ShortcutFolderSize.Large, folder.FolderSize);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void ResizeFolder_RejectsNonFolderWithoutSaving()
    {
        var shortcut = new IconInfo { IconId = "shortcut", Name = "App" };
        var menu = Group("group", shortcut);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.ResizeFolder,
            SubjectShortcutId: "shortcut",
            FolderSize: LauncherFolderSize.Large));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void MoveOutOfFolder_UsesStableIdsNormalizesFolderAndSaves()
    {
        var child = new IconInfo
        {
            IconId = "child",
            Name = "Child",
            LauncherGridColumn = 3,
            LauncherGridRow = 2
        };
        var remaining = new IconInfo { IconId = "remaining", Name = "Remaining" };
        IconInfo folder = ShortcutFolderService.CreateFolder("Tools");
        folder.IconId = "folder";
        folder.FolderItems.Add(child);
        folder.FolderItems.Add(remaining);
        var before = new IconInfo { IconId = "before", Name = "Before" };
        var menu = Group("group", before, folder);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.Execute(new LauncherMutationCommand(
            LauncherMutationKind.MoveOutOfFolder,
            TargetShortcutId: "folder",
            InsertIndex: 2,
            SubjectShortcutId: "child"));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { before, remaining, child }, menu.IconList.ToArray());
        Assert.AreEqual(-1, child.LauncherGridColumn);
        Assert.AreEqual(-1, child.LauncherGridRow);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateGridLayout_AtomicallyUpdatesMenuAndAllGroupsOwnedItemsWithOneSave()
    {
        var menuIcon = new IconInfo { IconId = "menu", Name = "Menu" };
        var ownedFolder = ShortcutFolderService.CreateFolder("Owned");
        ownedFolder.IconId = "owned";
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { Group("group", menuIcon) }
        };
        data.AppConfig.AllGroupsIconList.Add(ownedFolder);
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.UpdateGridLayout(
            new UpdateLauncherGridLayoutCommand(new[]
            {
                new LauncherGridPlacement("menu", 1, 2),
                new LauncherGridPlacement("owned", 3, 4)
            }));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(1, menuIcon.LauncherGridColumn);
        Assert.AreEqual(2, menuIcon.LauncherGridRow);
        Assert.AreEqual(3, ownedFolder.LauncherGridColumn);
        Assert.AreEqual(4, ownedFolder.LauncherGridRow);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateGridLayout_MissingIdRejectsWholeBatchWithoutPartialMutation()
    {
        var icon = new IconInfo { IconId = "existing", Name = "Existing" };
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { Group("group", icon) }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.UpdateGridLayout(
            new UpdateLauncherGridLayoutCommand(new[]
            {
                new LauncherGridPlacement("existing", 1, 1),
                new LauncherGridPlacement("missing", 2, 2)
            }));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(-1, icon.LauncherGridColumn);
        Assert.AreEqual(-1, icon.LauncherGridRow);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateOrder_GroupScopePreservesPlaceholderAndSavesOnce()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var placeholder = new IconInfo
        {
            IconId = "add",
            Name = "Add",
            IsAddShortcut_NoWrite = true
        };
        var menu = Group("group", first, placeholder, second);
        var store = new AppDataStore();
        store.Replace(new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        });
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.UpdateOrder(
            new UpdateLauncherShortcutOrderCommand(
                LauncherShortcutOrderScope.Group,
                new[] { "second", "first" },
                "group"));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { second, placeholder, first }, menu.IconList.ToArray());
        Assert.AreSame(placeholder, menu.IconList[1]);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateOrder_AllGroupsScopeUpdatesOnlyFlatKeysAndPreservesSourceGroups()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var third = new IconInfo { IconId = "third", Name = "Third" };
        var fourth = new IconInfo { IconId = "fourth", Name = "Fourth" };
        var firstGroup = Group("one", first, second);
        var secondGroup = Group("two", third, fourth);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { firstGroup, secondGroup }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.UpdateOrder(
            new UpdateLauncherShortcutOrderCommand(
                LauncherShortcutOrderScope.AllGroups,
                new[] { "fourth", "second", "third", "first" }));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { first, second }, firstGroup.IconList.ToArray());
        CollectionAssert.AreEqual(new[] { third, fourth }, secondGroup.IconList.ToArray());
        CollectionAssert.AreEqual(
            new[] { "id:fourth", "id:second", "id:third", "id:first" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void UpdateOrder_AllGroupsMissingIdRejectsBeforeChangingCollectionsOrKeys()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var menu = Group("group", first, second);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        };
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string> { "id:first", "id:second" };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.UpdateOrder(
            new UpdateLauncherShortcutOrderCommand(
                LauncherShortcutOrderScope.AllGroups,
                new[] { "second", "missing", "first" }));

        Assert.IsFalse(result.Success);
        CollectionAssert.AreEqual(new[] { first, second }, menu.IconList.ToArray());
        CollectionAssert.AreEqual(
            new[] { "id:first", "id:second" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    [TestMethod]
    public void MutateAllGroupsFolder_CreateKeepsSourceGroupsAndPersistsProjectionOnce()
    {
        var dragged = new IconInfo { IconId = "dragged", Name = "Dragged" };
        var target = new IconInfo { IconId = "target", Name = "Target" };
        var trailing = new IconInfo { IconId = "trailing", Name = "Trailing" };
        var menu = Group("group", dragged, target, trailing);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo> { menu }
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.MutateAllGroupsFolder(
            new LauncherAllGroupsFolderMutationCommand(
                LauncherAllGroupsFolderMutationKind.Create,
                "dragged",
                "target",
                new[] { "dragged", "target", "trailing" },
                InsertIndex: 1));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { dragged, target, trailing }, menu.IconList.ToArray());
        Assert.AreEqual(1, data.AppConfig.AllGroupsIconList.Count);
        IconInfo folder = data.AppConfig.AllGroupsIconList[0];
        Assert.IsTrue(folder.IsFolder);
        CollectionAssert.AreEqual(new[] { target, dragged }, folder.FolderItems.ToArray());
        CollectionAssert.AreEqual(
            new[] { "id:" + folder.IconId, "id:trailing" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void MutateAllGroupsFolder_MoveIntoHidesSubjectAndSavesOnce()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var subject = new IconInfo { IconId = "subject", Name = "Subject" };
        IconInfo folder = ShortcutFolderService.CreateFolder("Owned");
        folder.IconId = "folder";
        folder.FolderItems.Add(first);
        folder.FolderItems.Add(second);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                Group("group", first, second, subject)
            }
        };
        data.AppConfig.AllGroupsIconList.Add(folder);
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.MutateAllGroupsFolder(
            new LauncherAllGroupsFolderMutationCommand(
                LauncherAllGroupsFolderMutationKind.MoveInto,
                "subject",
                "folder",
                new[] { "folder", "subject" }));

        Assert.IsTrue(result.Success && result.Changed);
        CollectionAssert.AreEqual(new[] { first, second, subject }, data.MenuList[0].IconList.ToArray());
        CollectionAssert.AreEqual(new[] { first, second, subject }, folder.FolderItems.ToArray());
        CollectionAssert.AreEqual(
            new[] { "id:folder" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void MutateAllGroupsFolder_MoveOutUnwrapsSparseFolderAndPersistsFinalOrder()
    {
        var child = new IconInfo { IconId = "child", Name = "Child" };
        var remaining = new IconInfo { IconId = "remaining", Name = "Remaining" };
        var outside = new IconInfo { IconId = "outside", Name = "Outside" };
        IconInfo folder = ShortcutFolderService.CreateFolder("Owned");
        folder.IconId = "folder";
        folder.FolderItems.Add(child);
        folder.FolderItems.Add(remaining);
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                Group("group", child, remaining, outside)
            }
        };
        data.AppConfig.AllGroupsIconList.Add(folder);
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.MutateAllGroupsFolder(
            new LauncherAllGroupsFolderMutationCommand(
                LauncherAllGroupsFolderMutationKind.MoveOut,
                "child",
                "folder",
                new[] { "folder", "outside" },
                InsertIndex: 1));

        Assert.IsTrue(result.Success && result.Changed);
        Assert.AreEqual(0, data.AppConfig.AllGroupsIconList.Count);
        CollectionAssert.AreEqual(
            new[] { "id:remaining", "id:child", "id:outside" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(1, save.DebouncedCount);
    }

    [TestMethod]
    public void MutateAllGroupsFolder_StaleFlatOrderRejectsBeforeMutation()
    {
        var first = new IconInfo { IconId = "first", Name = "First" };
        var second = new IconInfo { IconId = "second", Name = "Second" };
        var third = new IconInfo { IconId = "third", Name = "Third" };
        var data = new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                Group("group", first, second, third)
            }
        };
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string>
        {
            "id:first",
            "id:second",
            "id:third"
        };
        var store = new AppDataStore();
        store.Replace(data);
        var save = new RecordingSaveService();
        var handler = new AppDataLauncherCommandHandler(store, save);

        LauncherCommandResult result = handler.MutateAllGroupsFolder(
            new LauncherAllGroupsFolderMutationCommand(
                LauncherAllGroupsFolderMutationKind.Create,
                "first",
                "second",
                new[] { "first", "second" },
                InsertIndex: 0));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(0, data.AppConfig.AllGroupsIconList.Count);
        CollectionAssert.AreEqual(new[] { first, second, third }, data.MenuList[0].IconList.ToArray());
        CollectionAssert.AreEqual(
            new[] { "id:first", "id:second", "id:third" },
            data.AppConfig.AllGroupsFlatIconOrderKeys);
        Assert.AreEqual(0, save.DebouncedCount);
    }

    private static MenuInfo Group(string id, params IconInfo[] icons) => new()
    {
        MenuId = id,
        MenuName = id,
        IconList = new ObservableCollection<IconInfo>(icons)
    };

    private sealed class RecordingSaveService : ISaveService
    {
        public int DebouncedCount { get; private set; }

        public void SaveNow() { }

        public void SaveDebounced() => DebouncedCount++;

        public void CancelPendingSave() { }

        public void Flush() { }
    }
}
