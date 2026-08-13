using rainspace.Features.Workspace.Presentation;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabScrollPlannerTests
{
    [TestMethod]
    public void TabLeftOfViewport_ScrollsToTabLeft()
    {
        WorkspaceGroupTabScrollPlan plan = WorkspaceGroupTabScrollPlanner.Plan(
            tabLeft: 40d,
            tabWidth: 80d,
            viewportOffset: 100d,
            viewportWidth: 300d,
            scrollableWidth: 500d);

        Assert.IsTrue(plan.ShouldScroll);
        Assert.AreEqual(40d, plan.TargetOffset);
    }

    [TestMethod]
    public void TabRightOfViewport_ScrollsMinimumDistance()
    {
        WorkspaceGroupTabScrollPlan plan = WorkspaceGroupTabScrollPlanner.Plan(
            tabLeft: 380d,
            tabWidth: 80d,
            viewportOffset: 100d,
            viewportWidth: 300d,
            scrollableWidth: 500d);

        Assert.IsTrue(plan.ShouldScroll);
        Assert.AreEqual(160d, plan.TargetOffset);
    }

    [TestMethod]
    public void VisibleTab_DoesNotScroll()
    {
        WorkspaceGroupTabScrollPlan plan = WorkspaceGroupTabScrollPlanner.Plan(
            tabLeft: 120d,
            tabWidth: 80d,
            viewportOffset: 100d,
            viewportWidth: 300d,
            scrollableWidth: 500d);

        Assert.IsFalse(plan.ShouldScroll);
    }

    [TestMethod]
    public void TargetOffset_IsClampedToScrollableRange()
    {
        WorkspaceGroupTabScrollPlan plan = WorkspaceGroupTabScrollPlanner.Plan(
            tabLeft: 490d,
            tabWidth: 100d,
            viewportOffset: 100d,
            viewportWidth: 300d,
            scrollableWidth: 200d);

        Assert.IsTrue(plan.ShouldScroll);
        Assert.AreEqual(200d, plan.TargetOffset);
    }
}
