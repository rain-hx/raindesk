using System.Collections.ObjectModel;
using rainspace.Features.Launcher.Presentation;
using rainspace.Services;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherAllGroupsProjectionBuilderTests
{
    [TestMethod]
    public void Build_PersistedStableIdsWinAndMissingVisibleItemsAreAppended()
    {
        var first = Icon("first");
        var second = Icon("second");
        var third = Icon("third");
        AppData data = Data(first, second, third);
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string>
        {
            "id:third",
            "id:first"
        };

        List<IconInfo> projection = LauncherAllGroupsProjectionBuilder.Build(data);

        CollectionAssert.AreEqual(new[] { third, first, second }, projection);
    }

    [TestMethod]
    public void Build_InSessionPreferredOrderWinsOverPersistedOrder()
    {
        var first = Icon("first");
        var second = Icon("second");
        AppData data = Data(first, second);
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string>
        {
            "id:first",
            "id:second"
        };

        List<IconInfo> projection = LauncherAllGroupsProjectionBuilder.Build(
            data,
            new[] { second, first });

        CollectionAssert.AreEqual(new[] { second, first }, projection);
    }

    [TestMethod]
    public void Build_FiltersMembersHiddenByOwnedFolderAndKeepsFolderRoot()
    {
        var hidden = Icon("hidden");
        var visible = Icon("visible");
        IconInfo folder = ShortcutFolderService.CreateFolder("Owned");
        folder.IconId = "folder";
        folder.FolderItems.Add(hidden);
        AppData data = Data(hidden, visible);
        data.AppConfig.AllGroupsIconList.Add(folder);
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string>
        {
            "id:hidden",
            "id:folder"
        };

        List<IconInfo> projection = LauncherAllGroupsProjectionBuilder.Build(data);

        CollectionAssert.AreEqual(new[] { folder, visible }, projection);
    }

    [TestMethod]
    public void Build_ResolvesLegacyMenuPathNameKey()
    {
        var icon = new IconInfo
        {
            IconId = "icon",
            Name = "Legacy",
            Path = "C:\\Apps\\legacy.exe"
        };
        AppData data = Data(icon);
        data.AppConfig.AllGroupsFlatIconOrderKeys = new List<string>
        {
            "group\u001fC:\\Apps\\legacy.exe\u001fLegacy"
        };

        List<IconInfo> projection = LauncherAllGroupsProjectionBuilder.Build(data);

        CollectionAssert.AreEqual(new[] { icon }, projection);
    }

    private static IconInfo Icon(string id) => new() { IconId = id, Name = id };

    private static AppData Data(params IconInfo[] icons) => new()
    {
        MenuList = new ObservableCollection<MenuInfo>
        {
            new()
            {
                MenuId = "group",
                IconList = new ObservableCollection<IconInfo>(icons)
            }
        }
    };
}
