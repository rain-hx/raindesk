using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherHorizontalAutoScrollPlannerTests
{
    [TestMethod]
    public void LeftEdge_PlansBackwardScroll()
    {
        LauncherHorizontalAutoScrollPlan plan = LauncherHorizontalAutoScrollPlanner.Plan(
            pointerX: 10d,
            viewportWidth: 300d,
            currentOffset: 100d,
            scrollableWidth: 500d,
            edgeThreshold: 36d,
            scrollStep: 28d);

        Assert.IsTrue(plan.ShouldScroll);
        Assert.AreEqual(72d, plan.TargetOffset);
    }

    [TestMethod]
    public void RightEdge_ClampsToScrollableWidth()
    {
        LauncherHorizontalAutoScrollPlan plan = LauncherHorizontalAutoScrollPlanner.Plan(
            pointerX: 290d,
            viewportWidth: 300d,
            currentOffset: 490d,
            scrollableWidth: 500d,
            edgeThreshold: 36d,
            scrollStep: 28d);

        Assert.IsTrue(plan.ShouldScroll);
        Assert.AreEqual(500d, plan.TargetOffset);
    }

    [TestMethod]
    public void Center_DoesNotScroll()
    {
        LauncherHorizontalAutoScrollPlan plan = LauncherHorizontalAutoScrollPlanner.Plan(
            pointerX: 150d,
            viewportWidth: 300d,
            currentOffset: 100d,
            scrollableWidth: 500d,
            edgeThreshold: 36d,
            scrollStep: 28d);

        Assert.IsFalse(plan.ShouldScroll);
        Assert.AreEqual(100d, plan.TargetOffset);
    }

    [TestMethod]
    public void LeftBoundaryAtZero_DoesNotRequestNoOpScroll()
    {
        LauncherHorizontalAutoScrollPlan plan = LauncherHorizontalAutoScrollPlanner.Plan(
            pointerX: 0d,
            viewportWidth: 300d,
            currentOffset: 0d,
            scrollableWidth: 500d,
            edgeThreshold: 36d,
            scrollStep: 28d);

        Assert.IsFalse(plan.ShouldScroll);
    }
}
