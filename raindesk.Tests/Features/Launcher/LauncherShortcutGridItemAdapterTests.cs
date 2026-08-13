using raindesk.Constant;
using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherShortcutGridItemAdapterTests
{
    [TestMethod]
    public void Create_MapsFolderSpanAndManualPosition()
    {
        var folder = new IconInfo
        {
            IconId = "folder",
            IsFolder = true,
            FolderSize = ShortcutFolderSize.Large,
            LauncherGridColumn = 3,
            LauncherGridRow = 2
        };

        var item = LauncherShortcutGridItemAdapter.Create(folder, columnCount: 4);

        Assert.AreSame(folder, item.Key);
        Assert.AreEqual(2, item.ColumnSpan);
        Assert.AreEqual(2, item.RowSpan);
        Assert.AreEqual(3, item.Column);
        Assert.AreEqual(2, item.Row);
    }

    [TestMethod]
    public void Create_UsesPlaceholderSpanAndClampsItToAvailableColumns()
    {
        var placeholder = new IconInfo
        {
            IsDragYieldPlaceholder_NoWrite = true,
            DragPlaceholderColumnSpan_NoWrite = 3,
            DragPlaceholderRowSpan_NoWrite = 2
        };

        var item = LauncherShortcutGridItemAdapter.Create(placeholder, columnCount: 2);

        Assert.AreEqual(2, item.ColumnSpan);
        Assert.AreEqual(2, item.RowSpan);
    }

    [TestMethod]
    public void Create_CanIgnorePersistedManualPositionForVirtualPreview()
    {
        var icon = new IconInfo
        {
            LauncherGridColumn = 2,
            LauncherGridRow = 3
        };

        var item = LauncherShortcutGridItemAdapter.Create(
            icon,
            columnCount: 4,
            preserveManualPosition: false);

        Assert.AreEqual(-1, item.Column);
        Assert.AreEqual(-1, item.Row);
    }
}
