using raindesk.Features.Launcher.Presentation;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherFolderNavigationStateTests
{
    [TestMethod]
    public void OpenChildAndBack_MaintainsFolderPath()
    {
        IconInfo root = ShortcutFolderService.CreateFolder("Root");
        IconInfo child = ShortcutFolderService.CreateFolder("Child");
        var state = new LauncherFolderNavigationState();

        state.OpenRoot(root);
        state.OpenChild(child);

        Assert.IsTrue(state.IsOpen);
        Assert.IsTrue(state.CanGoBack);
        Assert.AreSame(child, state.Folder);
        CollectionAssert.AreEqual(new[] { root, child }, state.Path.ToArray());

        state.Back();

        Assert.IsFalse(state.CanGoBack);
        Assert.AreSame(root, state.Folder);
    }

    [TestMethod]
    public void OpenRoot_NonFolderClosesExistingPath()
    {
        var state = new LauncherFolderNavigationState();
        state.OpenRoot(ShortcutFolderService.CreateFolder("Folder"));

        state.OpenRoot(new IconInfo { Name = "Shortcut" });

        Assert.IsFalse(state.IsOpen);
        Assert.AreEqual(0, state.Path.Count);
    }

    [TestMethod]
    public void Close_ClearsNestedPath()
    {
        var state = new LauncherFolderNavigationState();
        state.OpenRoot(ShortcutFolderService.CreateFolder("Root"));
        state.OpenChild(ShortcutFolderService.CreateFolder("Child"));

        state.Close();

        Assert.IsFalse(state.IsOpen);
        Assert.IsNull(state.Folder);
        Assert.AreEqual(0, state.Path.Count);
    }
}
