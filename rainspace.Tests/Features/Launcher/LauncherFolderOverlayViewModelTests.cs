using System.ComponentModel;
using rainspace.Features.Launcher.Presentation;
using rainspace.Services;
using rainspace.ViewModel;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherFolderOverlayViewModelTests
{
    [TestMethod]
    public void OpenNestedFolder_ProjectsTitleItemsAndBackState()
    {
        IconInfo root = ShortcutFolderService.CreateFolder("Root");
        IconInfo child = ShortcutFolderService.CreateFolder("Child");
        root.FolderItems.Add(child);
        var viewModel = new LauncherFolderOverlayViewModel();

        viewModel.OpenRoot(root);
        viewModel.OpenChild(child);

        Assert.IsTrue(viewModel.IsOpen);
        Assert.IsTrue(viewModel.CanGoBack);
        Assert.AreEqual("Root / Child", viewModel.Title);
        Assert.AreSame(child.FolderItems, viewModel.Items);
    }

    [TestMethod]
    public void BackAndClose_PublishCompleteProjection()
    {
        IconInfo root = ShortcutFolderService.CreateFolder("Root");
        IconInfo child = ShortcutFolderService.CreateFolder("Child");
        var viewModel = new LauncherFolderOverlayViewModel();
        viewModel.OpenRoot(root);
        viewModel.OpenChild(child);
        var changed = new HashSet<string>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName ?? string.Empty);

        viewModel.Back();
        viewModel.Close();

        Assert.IsFalse(viewModel.IsOpen);
        Assert.AreEqual(string.Empty, viewModel.Title);
        Assert.IsNull(viewModel.Items);
        CollectionAssert.IsSubsetOf(
            new[]
            {
                nameof(LauncherFolderOverlayViewModel.Folder),
                nameof(LauncherFolderOverlayViewModel.Items),
                nameof(LauncherFolderOverlayViewModel.IsOpen),
                nameof(LauncherFolderOverlayViewModel.CanGoBack),
                nameof(LauncherFolderOverlayViewModel.Title)
            },
            changed.ToArray());
    }
}
