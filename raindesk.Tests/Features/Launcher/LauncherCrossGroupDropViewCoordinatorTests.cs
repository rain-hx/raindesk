using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupDropViewCoordinatorTests
{
    [TestMethod]
    public void Apply_AllGroupsWithCapturedOrder_RestoresProjectionBeforePersisting()
    {
        var calls = new List<string>();
        var order = new[] { "a", "b" };
        LauncherCrossGroupDropViewCoordinator<string> coordinator = CreateCoordinator(
            calls,
            () => order);

        LauncherCrossGroupDropViewUpdate update = coordinator.Apply(
            wasAllGroups: true,
            activeGroupIndex: 3,
            beforePersist: () => calls.Add("cleanup"));

        CollectionAssert.AreEqual(
            new[] { "select-all", "apply:a,b", "layout", "selection:-1", "cleanup", "persist" },
            calls);
        Assert.IsTrue(update.RestoredAllGroups);
        Assert.IsTrue(update.AppliedCapturedOrder);
        Assert.AreEqual(-1, update.SelectedGroupIndex);
    }

    [TestMethod]
    public void Apply_AllGroupsWithoutCapturedOrder_RebuildsProjection()
    {
        var calls = new List<string>();
        LauncherCrossGroupDropViewCoordinator<string> coordinator = CreateCoordinator(
            calls,
            static () => Array.Empty<string>());

        LauncherCrossGroupDropViewUpdate update = coordinator.Apply(true, activeGroupIndex: 2);

        CollectionAssert.AreEqual(
            new[] { "select-all", "rebuild", "layout", "selection:-1", "persist" },
            calls);
        Assert.IsFalse(update.AppliedCapturedOrder);
    }

    [TestMethod]
    public void Apply_ActiveGroup_RebuildsAndKeepsActiveSelection()
    {
        var calls = new List<string>();
        int orderReads = 0;
        LauncherCrossGroupDropViewCoordinator<string> coordinator = CreateCoordinator(
            calls,
            () =>
            {
                orderReads++;
                return new[] { "unused" };
            });

        LauncherCrossGroupDropViewUpdate update = coordinator.Apply(false, activeGroupIndex: 4);

        CollectionAssert.AreEqual(
            new[] { "rebuild", "layout", "selection:4", "persist" },
            calls);
        Assert.AreEqual(0, orderReads);
        Assert.IsFalse(update.RestoredAllGroups);
        Assert.AreEqual(4, update.SelectedGroupIndex);
    }

    private static LauncherCrossGroupDropViewCoordinator<string> CreateCoordinator(
        ICollection<string> calls,
        Func<IReadOnlyList<string>?> getOrder) =>
        new(
            getOrder,
            () => calls.Add("select-all"),
            order => calls.Add("apply:" + string.Join(',', order)),
            () => calls.Add("rebuild"),
            () => calls.Add("layout"),
            index => calls.Add($"selection:{index}"),
            () => calls.Add("persist"));
}
