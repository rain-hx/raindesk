using raindesk.Constant;
using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests;

[TestClass]
public class LauncherGridOrganizerTests
{
    [TestMethod]
    public void Plan_ReturnsStableIdPlacementsWithoutMutatingModels()
    {
        var first = new IconInfo
        {
            IconId = "first",
            LauncherGridColumn = 3,
            LauncherGridRow = 2
        };
        var second = new IconInfo
        {
            IconId = "second",
            LauncherGridColumn = -1,
            LauncherGridRow = -1
        };

        IReadOnlyList<LauncherGridPlacement> plan = LauncherGridOrganizer.Plan(
            new[] { first, second },
            columnCount: 4);

        Assert.AreEqual(2, plan.Count);
        Assert.AreEqual(new LauncherGridPlacement("first", 0, 0), plan[0]);
        Assert.AreEqual(new LauncherGridPlacement("second", 1, 0), plan[1]);
        Assert.AreEqual(3, first.LauncherGridColumn);
        Assert.AreEqual(2, first.LauncherGridRow);
        Assert.AreEqual(-1, second.LauncherGridColumn);
        Assert.AreEqual(-1, second.LauncherGridRow);
    }

    [TestMethod]
    public void Organize_ScatteredIcons_CompactsThemInVisualOrder()
    {
        var manual = new IconInfo { LauncherGridColumn = 2, LauncherGridRow = 1 };
        var first = new IconInfo();
        var second = new IconInfo();

        LauncherGridOrganizeResult result = LauncherGridOrganizer.Organize(
            new[] { manual, first, second },
            columnCount: 4);

        Assert.AreEqual(3, result.OrganizedCount);
        Assert.AreEqual(0, manual.LauncherGridColumn);
        Assert.AreEqual(0, manual.LauncherGridRow);
        Assert.AreEqual(1, first.LauncherGridColumn);
        Assert.AreEqual(0, first.LauncherGridRow);
        Assert.AreEqual(2, second.LauncherGridColumn);
        Assert.AreEqual(0, second.LauncherGridRow);
    }

    [TestMethod]
    public void Organize_ConflictingManualIcons_MovesLaterIconWithoutOverlap()
    {
        var first = new IconInfo { LauncherGridColumn = 1, LauncherGridRow = 0 };
        var second = new IconInfo { LauncherGridColumn = 1, LauncherGridRow = 0 };

        LauncherGridOrganizeResult result = LauncherGridOrganizer.Organize(
            new[] { first, second },
            columnCount: 3);

        Assert.AreEqual(1, result.OrganizedCount);
        Assert.AreEqual(0, first.LauncherGridColumn);
        Assert.AreEqual(0, first.LauncherGridRow);
        Assert.AreNotEqual(
            new LauncherGridCell(first.LauncherGridColumn, first.LauncherGridRow),
            new LauncherGridCell(second.LauncherGridColumn, second.LauncherGridRow));
    }

    [TestMethod]
    public void Organize_LargeFolder_ReservesItsFullSpan()
    {
        var folder = new IconInfo
        {
            IsFolder = true,
            FolderSize = ShortcutFolderSize.Large,
            LauncherGridColumn = 0,
            LauncherGridRow = 0,
        };
        var unpositioned = new IconInfo();

        LauncherGridOrganizer.Organize(new[] { folder, unpositioned }, columnCount: 4);

        Assert.AreEqual(2, unpositioned.LauncherGridColumn);
        Assert.AreEqual(0, unpositioned.LauncherGridRow);
    }

    [TestMethod]
    public void Organize_ScatteredIcons_PreservesTheirVisualOrder()
    {
        var lower = new IconInfo { Name = "lower", LauncherGridColumn = 0, LauncherGridRow = 3 };
        var upperRight = new IconInfo { Name = "upper-right", LauncherGridColumn = 4, LauncherGridRow = 0 };
        var upperLeft = new IconInfo { Name = "upper-left", LauncherGridColumn = 1, LauncherGridRow = 0 };

        LauncherGridOrganizer.Organize(
            new[] { lower, upperRight, upperLeft },
            columnCount: 5);

        Assert.AreEqual(0, upperLeft.LauncherGridColumn);
        Assert.AreEqual(1, upperRight.LauncherGridColumn);
        Assert.AreEqual(2, lower.LauncherGridColumn);
        Assert.AreEqual(0, upperLeft.LauncherGridRow);
        Assert.AreEqual(0, upperRight.LauncherGridRow);
        Assert.AreEqual(0, lower.LauncherGridRow);
    }

    [TestMethod]
    public void Organize_ReorderMode_UsesRequestedListOrderInsteadOfOldCoordinates()
    {
        var first = new IconInfo { Name = "first", LauncherGridColumn = 0, LauncherGridRow = 0 };
        var second = new IconInfo { Name = "second", LauncherGridColumn = 1, LauncherGridRow = 0 };

        LauncherGridOrganizer.Organize(
            new[] { second, first },
            columnCount: 4,
            preserveExistingVisualOrder: false);

        Assert.AreEqual(0, second.LauncherGridColumn);
        Assert.AreEqual(1, first.LauncherGridColumn);
    }
}
