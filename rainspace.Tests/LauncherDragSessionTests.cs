using rainspace.Constant;
using rainspace.Features.Launcher.Application;
using rainspace.Features.Launcher.Presentation;
using rainspace.Services;
using rainspace.ViewModel;

namespace rainspace.Tests;

[TestClass]
public class LauncherDragSessionTests
{
    [TestMethod]
    public void ResolvePointer_NormalTargetTakesCreateFolderPriorityOverReorder()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { dragged, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 80, 80)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 0, 80, 80))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(140, 40));

        Assert.AreEqual(LauncherDropKind.CreateFolderTarget, preview.Kind);
        Assert.AreSame(target, preview.TargetIcon);
        Assert.IsFalse(preview.ShouldReorder);
    }

    [TestMethod]
    public void ResolvePointer_NormalTargetEdgeUsesReorder()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { dragged, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 88, 94)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(88, 0, 88, 94))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(90, 4));

        Assert.AreEqual(LauncherDropKind.ReorderTarget, preview.Kind);
        Assert.IsNull(preview.TargetIcon);
        Assert.IsTrue(preview.ShouldReorder);
    }

    [TestMethod]
    public void ResolvePointer_NormalTargetCenterNeverReorders()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { dragged, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 88, 94)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(88, 0, 88, 94))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(132, 47));

        Assert.AreEqual(LauncherDropKind.CreateFolderTarget, preview.Kind);
        Assert.IsFalse(preview.ShouldReorder);
    }

    [TestMethod]
    public void ResolvePointer_FolderOnNormalTargetCenterUsesReorder()
    {
        var draggedFolder = ShortcutFolderService.CreateFolder("Dragged");
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { draggedFolder, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [draggedFolder] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 88, 94)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(88, 0, 88, 94))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { draggedFolder },
            layout,
            new LauncherPoint(140, 47));

        Assert.AreEqual(LauncherDropKind.ReorderTarget, preview.Kind);
        Assert.IsTrue(preview.ShouldReorder);
        Assert.AreEqual(1, preview.ReorderIndex);
    }

    [TestMethod]
    public void ResolvePointer_FolderOnFolderUsesReorderInsteadOfMoveIntoFolder()
    {
        var draggedFolder = ShortcutFolderService.CreateFolder("Dragged");
        var targetFolder = ShortcutFolderService.CreateFolder("Target");
        var tileOrder = new[] { draggedFolder, targetFolder };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [draggedFolder] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 80, 80)),
            [targetFolder] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 0, 80, 80))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { draggedFolder },
            layout,
            new LauncherPoint(140, 40));

        Assert.AreNotEqual(LauncherDropKind.FolderTarget, preview.Kind);
        Assert.IsNull(preview.TargetIcon);
    }

    [TestMethod]
    public void ResolvePointer_FolderTargetTakesMoveIntoFolderPriorityOverReorder()
    {
        var dragged = new IconInfo { Name = "A" };
        var folder = ShortcutFolderService.CreateFolder("Folder");
        var tileOrder = new[] { dragged, folder };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 80, 80)),
            [folder] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 0, 80, 80))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(140, 40));

        Assert.AreEqual(LauncherDropKind.FolderTarget, preview.Kind);
        Assert.AreSame(folder, preview.TargetIcon);
        Assert.IsFalse(preview.ShouldReorder);
    }

    [TestMethod]
    public void ResolvePointer_EmptyAreaReturnsReorderTarget()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { dragged, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 80, 80)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 0, 80, 80))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(20, 120));

        Assert.AreEqual(LauncherDropKind.ReorderTarget, preview.Kind);
        Assert.IsTrue(preview.ShouldReorder);
        Assert.AreEqual(1, preview.ReorderIndex);
    }

    [TestMethod]
    public void ResolvePointer_ReorderIndexExcludesDraggedIconBeforeCenterComparison()
    {
        var iconA = new IconInfo { Name = "A" };
        var dragged = new IconInfo { Name = "B" };
        var iconC = new IconInfo { Name = "C" };
        var iconD = new IconInfo { Name = "D" };
        var tileOrder = new[] { iconA, dragged, iconC, iconD };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [iconA] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 88, 94)),
            [dragged] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(88, 0, 88, 94)),
            [iconC] = new(new LauncherGridCell(2, 0), 1, 1, new LauncherRect(176, 0, 88, 94)),
            [iconD] = new(new LauncherGridCell(3, 0), 1, 1, new LauncherRect(264, 0, 88, 94))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(180, 47),
            tileLimit: 3);

        Assert.AreEqual(LauncherDropKind.ReorderTarget, preview.Kind);
        Assert.AreEqual(1, preview.ReorderIndex);
    }

    [TestMethod]
    public void ResolvePointer_TargetHotZoneUsesCreateFolderWhenOverTargetBounds()
    {
        var dragged = new IconInfo { Name = "A" };
        var target = new IconInfo { Name = "B" };
        var tileOrder = new[] { dragged, target };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 96, 76)),
            [target] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(96, 0, 96, 76))
        };

        var preview = LauncherDragSession.ResolvePointer(
            tileOrder,
            new[] { dragged },
            layout,
            new LauncherPoint(144, 38));

        Assert.AreEqual(LauncherDropKind.CreateFolderTarget, preview.Kind);
        Assert.AreSame(target, preview.TargetIcon);
        Assert.IsFalse(preview.ShouldReorder);
    }

    [TestMethod]
    public void ResolveListReorderIndex_SpannedFolderUsesListIndexNotVisualSortIndex()
    {
        var iconA = new IconInfo { Name = "A" };
        var folder = ShortcutFolderService.CreateFolder("Folder");
        folder.FolderSize = ShortcutFolderSize.Large;
        var iconB = new IconInfo { Name = "B" };
        var iconC = new IconInfo { Name = "C" };
        var tileOrder = new[] { iconA, folder, iconB, iconC };
        var metrics = new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d);
        var items = new[]
        {
            new LauncherGridItem(iconA, 1, 1),
            new LauncherGridItem(folder, 2, 2),
            new LauncherGridItem(iconB, 1, 1),
            new LauncherGridItem(iconC, 1, 1)
        };
        var engineLayout = LauncherGridLayoutEngine.Arrange(items, metrics);
        var layout = engineLayout.ToDictionary(pair => (IconInfo)pair.Key, pair => pair.Value);

        int insertIndex = LauncherDragSession.ResolveListReorderIndex(
            tileOrder,
            layout,
            new LauncherPoint(50d, 90d),
            tileLimit: 3);

        Assert.AreEqual(2, insertIndex, "Pointer over B row should insert before list item B, not folder.");
    }

    [TestMethod]
    public void ResolveGridReorderIndex_PointerInsideSpannedFolderUsesFolderAsTarget()
    {
        var iconA = new IconInfo { Name = "A" };
        var dragged = new IconInfo { Name = "Dragged" };
        var folder = ShortcutFolderService.CreateFolder("Folder");
        folder.FolderSize = ShortcutFolderSize.Large;
        var iconB = new IconInfo { Name = "B" };
        var iconC = new IconInfo { Name = "C" };
        var tileOrder = new[] { iconA, dragged, folder, iconB, iconC };
        var metrics = new LauncherGridMetrics(ColumnCount: 3, SlotWidth: 100d, SlotHeight: 80d);
        var items = new[]
        {
            new LauncherGridItem(iconA, 1, 1),
            new LauncherGridItem(dragged, 1, 1),
            new LauncherGridItem(folder, 2, 2),
            new LauncherGridItem(iconB, 1, 1),
            new LauncherGridItem(iconC, 1, 1)
        };
        var engineLayout = LauncherGridLayoutEngine.Arrange(items, metrics);
        var layout = engineLayout.ToDictionary(pair => (IconInfo)pair.Key, pair => pair.Value);

        int insertIndex = LauncherDragSession.ResolveGridReorderIndex(
            tileOrder,
            layout,
            new LauncherPoint(20d, 100d),
            tileLimit: 4,
            new HashSet<IconInfo> { dragged });

        Assert.AreEqual(2, insertIndex);
    }

    [TestMethod]
    public void ResolveGridReorderIndex_RightwardGapBeforeNextIconDoesNotSkipMultipleIcons()
    {
        var dragged = new IconInfo { Name = "Dragged" };
        var iconB = new IconInfo { Name = "B" };
        var iconC = new IconInfo { Name = "C" };
        var iconD = new IconInfo { Name = "D" };
        var tileOrder = new[] { dragged, iconB, iconC, iconD };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(0, 0, 64, 94)),
            [iconB] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 0, 64, 94)),
            [iconC] = new(new LauncherGridCell(2, 0), 1, 1, new LauncherRect(200, 0, 64, 94)),
            [iconD] = new(new LauncherGridCell(3, 0), 1, 1, new LauncherRect(300, 0, 64, 94))
        };

        int insertIndex = LauncherDragSession.ResolveGridReorderIndex(
            tileOrder,
            layout,
            new LauncherPoint(184d, 47d),
            tileLimit: 3,
            new HashSet<IconInfo> { dragged });

        Assert.AreEqual(1, insertIndex);
    }

    [TestMethod]
    public void ResolveGridReorderIndex_ExcludedFirstIconKeepsFullGridGeometry()
    {
        var dragged = new IconInfo { Name = "Dragged" };
        var second = new IconInfo { Name = "Second" };
        var third = new IconInfo { Name = "Third" };
        var order = new[] { dragged, second, third };
        var layout = new Dictionary<IconInfo, LauncherGridRect>
        {
            [dragged] = new(new LauncherGridCell(0, 0), 1, 1, new LauncherRect(20, 10, 80, 90)),
            [second] = new(new LauncherGridCell(1, 0), 1, 1, new LauncherRect(100, 10, 80, 90)),
            [third] = new(new LauncherGridCell(2, 0), 1, 1, new LauncherRect(180, 10, 80, 90))
        };

        int insertIndex = LauncherDragSession.ResolveGridReorderIndex(
            order,
            layout,
            new LauncherPoint(275, 50),
            tileLimit: 2,
            new HashSet<IconInfo> { dragged });

        Assert.AreEqual(2, insertIndex);
    }
}
