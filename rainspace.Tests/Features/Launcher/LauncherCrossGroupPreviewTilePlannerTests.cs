using rainspace.Features.Launcher.Presentation;

namespace rainspace.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupPreviewTilePlannerTests
{
    [TestMethod]
    public void Plan_RemovesDraggedItemsFromTargetAndAppendsThemInDragOrder()
    {
        string[] target = { "a", "b", "c" };
        string[] dragged = { "b", "d" };

        LauncherCrossGroupPreviewTilePlan<string> plan =
            LauncherCrossGroupPreviewTilePlanner.Plan(
                target,
                dragged,
                static _ => false);

        CollectionAssert.AreEqual(
            new[] { "a", "c", "b", "d" },
            plan.Items.ToArray());
        Assert.AreEqual(2, plan.TargetInsertIndex);
    }

    [TestMethod]
    public void Plan_ExcludesPlaceholdersFromTargetAndDraggedItems()
    {
        string[] target = { "a", "placeholder", "b" };
        string[] dragged = { "placeholder", "c" };

        LauncherCrossGroupPreviewTilePlan<string> plan =
            LauncherCrossGroupPreviewTilePlanner.Plan(
                target,
                dragged,
                static item => item == "placeholder");

        CollectionAssert.AreEqual(new[] { "a", "b", "c" }, plan.Items.ToArray());
        Assert.AreEqual(2, plan.TargetInsertIndex);
    }

    [TestMethod]
    public void Plan_WithNoTarget_InsertsDraggedItemsAtStart()
    {
        LauncherCrossGroupPreviewTilePlan<string> plan =
            LauncherCrossGroupPreviewTilePlanner.Plan(
                targetItems: null,
                draggedItems: new[] { "a", "b" },
                exclude: static _ => false);

        CollectionAssert.AreEqual(new[] { "a", "b" }, plan.Items.ToArray());
        Assert.AreEqual(0, plan.TargetInsertIndex);
    }

    [TestMethod]
    public void Plan_DoesNotMutateInputCollections()
    {
        var target = new List<string> { "a", "b" };
        var dragged = new List<string> { "b" };

        _ = LauncherCrossGroupPreviewTilePlanner.Plan(
            target,
            dragged,
            static _ => false);

        CollectionAssert.AreEqual(new[] { "a", "b" }, target);
        CollectionAssert.AreEqual(new[] { "b" }, dragged);
    }
}
