using raindesk.Constant;
using raindesk.Features.Launcher.Presentation;
using raindesk.ViewModel;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherBatchPlaceholderCoordinatorTests
{
    [TestMethod]
    public void TryBegin_ReplacesNonContiguousIconsAndPreservesVisualSpans()
    {
        IconInfo before = Icon("before");
        IconInfo first = Icon("first");
        IconInfo middle = Icon("middle");
        IconInfo folder = Icon("folder");
        folder.IsFolder = true;
        folder.FolderSize = ShortcutFolderSize.Large;
        IconInfo after = Icon("after");
        var tiles = new List<IconInfo> { before, first, middle, folder, after };
        var coordinator = new LauncherBatchPlaceholderCoordinator();

        bool started = coordinator.TryBegin(tiles, new[] { folder, first });

        Assert.IsTrue(started);
        Assert.IsTrue(coordinator.State.IsSuppressedFromTiles);
        Assert.AreEqual(1, coordinator.State.InsertIndex);
        Assert.AreEqual(1, coordinator.State.OriginalIndices[first]);
        Assert.AreEqual(3, coordinator.State.OriginalIndices[folder]);
        Assert.AreEqual(2, coordinator.State.YieldPlaceholders.Count);
        CollectionAssert.AreEqual(
            new[]
            {
                before,
                coordinator.State.YieldPlaceholders[0],
                coordinator.State.YieldPlaceholders[1],
                middle,
                after
            },
            tiles);
        Assert.AreEqual(2, coordinator.State.YieldPlaceholders[0].BatchDragGroupCount_NoWrite);
        Assert.AreEqual(1, coordinator.State.YieldPlaceholders[0].DragPlaceholderColumnSpan_NoWrite);
        Assert.AreEqual(2, coordinator.State.YieldPlaceholders[1].DragPlaceholderColumnSpan_NoWrite);
        Assert.AreEqual(2, coordinator.State.YieldPlaceholders[1].DragPlaceholderRowSpan_NoWrite);
    }

    [TestMethod]
    public void ApplyRestore_WhenCancelled_RestoresExactOriginalOrder()
    {
        IconInfo first = Icon("first");
        IconInfo draggedA = Icon("dragged-a");
        IconInfo middle = Icon("middle");
        IconInfo draggedB = Icon("dragged-b");
        IconInfo last = Icon("last");
        var original = new[] { first, draggedA, middle, draggedB, last };
        var tiles = original.ToList();
        var coordinator = new LauncherBatchPlaceholderCoordinator();
        coordinator.TryBegin(tiles, new[] { draggedB, draggedA });

        LauncherBatchRestorePlan plan = coordinator.PlanRestore(tiles, cancelled: true)!;
        coordinator.ApplyRestore(tiles, plan);

        CollectionAssert.AreEqual(original, tiles);
        Assert.IsFalse(coordinator.State.IsSuppressedFromTiles);
        Assert.AreEqual(0, coordinator.State.YieldPlaceholders.Count);
    }

    [TestMethod]
    public void ApplyRestore_WhenCommitted_InsertsDraggedIconsAtPlaceholderSlot()
    {
        IconInfo first = Icon("first");
        IconInfo draggedA = Icon("dragged-a");
        IconInfo middle = Icon("middle");
        IconInfo draggedB = Icon("dragged-b");
        IconInfo last = Icon("last");
        var tiles = new List<IconInfo> { first, draggedA, middle, draggedB, last };
        var coordinator = new LauncherBatchPlaceholderCoordinator();
        coordinator.TryBegin(tiles, new[] { draggedB, draggedA });

        foreach (IconInfo placeholder in coordinator.State.YieldPlaceholders.ToArray())
        {
            tiles.Remove(placeholder);
        }
        tiles.InsertRange(2, coordinator.State.YieldPlaceholders);
        coordinator.State.InsertIndex = 2;

        LauncherBatchRestorePlan plan = coordinator.PlanRestore(tiles, cancelled: false)!;
        coordinator.ApplyRestore(tiles, plan);

        CollectionAssert.AreEqual(
            new[] { first, middle, draggedA, draggedB, last },
            tiles);
        Assert.AreEqual(-1, coordinator.State.InsertIndex);
    }

    [TestMethod]
    public void BuildTargetPreview_RemovesDraggedDuplicatesAndClampsInsertIndex()
    {
        IconInfo dragged = Icon("dragged");
        var source = new List<IconInfo> { dragged };
        var coordinator = new LauncherBatchPlaceholderCoordinator();
        coordinator.TryBegin(source, new[] { dragged });
        IconInfo targetFirst = Icon("target-first");
        IconInfo targetLast = Icon("target-last");

        List<IconInfo> preview = coordinator.BuildTargetPreview(
            new[] { targetFirst, dragged, targetLast },
            new[] { dragged },
            requestedInsertIndex: 99);

        CollectionAssert.AreEqual(
            new[] { targetFirst, targetLast, coordinator.State.YieldPlaceholders[0] },
            preview);
        Assert.AreEqual(2, coordinator.State.InsertIndex);
    }

    [TestMethod]
    public void RestrictAndOrphanCleanup_OperateWithoutUiTypes()
    {
        IconInfo dragged = Icon("dragged");
        var tiles = new List<IconInfo> { dragged };
        var coordinator = new LauncherBatchPlaceholderCoordinator();
        coordinator.TryBegin(tiles, new[] { dragged });
        var dragItems = new List<object> { dragged, Icon("extra") };

        Assert.IsTrue(coordinator.RestrictDragItemsToPrimaryPlaceholder(dragItems));
        Assert.AreEqual(1, dragItems.Count);
        Assert.AreSame(coordinator.State.YieldPlaceholders[0], dragItems[0]);

        tiles.Add(new IconInfo { IsDragYieldPlaceholder_NoWrite = true });
        Assert.AreEqual(2, LauncherBatchPlaceholderCoordinator.RemoveOrphanPlaceholders(tiles));
        Assert.AreEqual(0, tiles.Count);
    }

    private static IconInfo Icon(string id) => new() { IconId = id, Name = id };
}
