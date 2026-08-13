using raindesk.Features.Workspace.Presentation;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabInsertPlannerTests
{
    private static readonly WorkspaceGroupTabSlot[] Slots =
    {
        new(0, 0d, 60d, false),
        new(1, 60d, 80d, false),
        new(2, 140d, 80d, true),
        new(3, 220d, 80d, false)
    };

    [TestMethod]
    public void InitialResolve_NeverTargetsAllGroupsIndex()
    {
        int target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 10d,
            itemCount: 4,
            Slots,
            isActiveDrag: false,
            currentIndex: 2,
            hysteresis: 14d);

        Assert.AreEqual(1, target);
    }

    [TestMethod]
    public void InitialResolve_SkipsDraggedSlotAndUsesMidpoints()
    {
        int target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 190d,
            itemCount: 4,
            Slots,
            isActiveDrag: false,
            currentIndex: 2,
            hysteresis: 14d);

        Assert.AreEqual(3, target);
    }

    [TestMethod]
    public void ActiveDrag_StaysUntilPreviousBottomEdgeHysteresisIsCrossed()
    {
        int target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 155d,
            itemCount: 4,
            Slots,
            isActiveDrag: true,
            currentIndex: 2,
            hysteresis: 14d);

        Assert.AreEqual(2, target);

        target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 153d,
            itemCount: 4,
            Slots,
            isActiveDrag: true,
            currentIndex: 2,
            hysteresis: 14d);
        Assert.AreEqual(1, target);
    }

    [TestMethod]
    public void ActiveDrag_StaysUntilNextTopEdgeHysteresisIsCrossed()
    {
        int target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 205d,
            itemCount: 4,
            Slots,
            isActiveDrag: true,
            currentIndex: 2,
            hysteresis: 14d);

        Assert.AreEqual(2, target);

        target = WorkspaceGroupTabInsertPlanner.Resolve(
            pointerY: 207d,
            itemCount: 4,
            Slots,
            isActiveDrag: true,
            currentIndex: 2,
            hysteresis: 14d);
        Assert.AreEqual(3, target);
    }

    [TestMethod]
    public void EmptyWorkspace_ReturnsNoTarget()
    {
        Assert.AreEqual(-1, WorkspaceGroupTabInsertPlanner.Resolve(
            0d,
            itemCount: 1,
            Array.Empty<WorkspaceGroupTabSlot>(),
            isActiveDrag: false,
            currentIndex: -1,
            hysteresis: 14d));
    }
}
