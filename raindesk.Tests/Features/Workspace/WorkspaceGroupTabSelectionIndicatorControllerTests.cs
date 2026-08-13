using raindesk.Features.Workspace.Presentation.WinUI;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabSelectionIndicatorControllerTests
{
    [TestMethod]
    public void BeginRefresh_AnimatesOnlyAfterInitialLayout()
    {
        var controller = new WorkspaceGroupTabSelectionIndicatorController();

        Assert.IsFalse(controller.BeginRefresh(animationsEnabled: true));
        Assert.IsTrue(controller.BeginRefresh(animationsEnabled: true));
        Assert.IsFalse(controller.BeginRefresh(animationsEnabled: false));
    }

    [TestMethod]
    [DataRow(true, 0, false, true, true)]
    [DataRow(false, 0, false, true, false)]
    [DataRow(true, 1, false, true, false)]
    [DataRow(true, 0, true, true, false)]
    [DataRow(true, 0, false, false, false)]
    public void ShouldAnimate_RequiresInitialNonEditingAllowedUpdate(
        bool requested,
        int attempt,
        bool editing,
        bool animationsAllowed,
        bool expected)
    {
        Assert.AreEqual(
            expected,
            WorkspaceGroupTabSelectionIndicatorController.ShouldAnimate(
                requested,
                attempt,
                editing,
                animationsAllowed));
    }

    [TestMethod]
    [DataRow(0, 10, true)]
    [DataRow(9, 10, true)]
    [DataRow(10, 10, false)]
    [DataRow(0, 0, false)]
    public void ShouldRetry_StopsAtConfiguredLimit(
        int attempt,
        int maxAttempts,
        bool expected)
    {
        Assert.AreEqual(
            expected,
            WorkspaceGroupTabSelectionIndicatorController.ShouldRetry(
                attempt,
                maxAttempts));
    }
}
