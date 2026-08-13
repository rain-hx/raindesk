using System.Collections.ObjectModel;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Services;

[TestClass]
public class ShortcutIdentityServiceTests
{
    [TestMethod]
    public void EnsureIconIds_AssignsIdsRecursively()
    {
        var child = new IconInfo { Name = "Child", IconId = "" };
        var folder = new IconInfo { Name = "Folder", IsFolder = true, IconId = "" };
        folder.FolderItems.Add(child);
        var data = CreateAppData(folder);

        bool changed = ShortcutIdentityService.EnsureIconIds(data);

        Assert.IsTrue(changed);
        Assert.IsFalse(string.IsNullOrWhiteSpace(folder.IconId));
        Assert.IsFalse(string.IsNullOrWhiteSpace(child.IconId));
        Assert.AreNotEqual(folder.IconId, child.IconId);
    }

    [TestMethod]
    public void EnsureIconIds_ReplacesDuplicateIds()
    {
        var first = new IconInfo { Name = "First", IconId = "same" };
        var second = new IconInfo { Name = "Second", IconId = "same" };
        var data = CreateAppData(first, second);

        bool changed = ShortcutIdentityService.EnsureIconIds(data);

        Assert.IsTrue(changed);
        Assert.AreEqual("same", first.IconId);
        Assert.AreNotEqual(first.IconId, second.IconId);
    }

    private static AppData CreateAppData(params IconInfo[] icons)
    {
        return new AppData
        {
            MenuList = new ObservableCollection<MenuInfo>
            {
                new() { IconList = new ObservableCollection<IconInfo>(icons) }
            }
        };
    }
}
