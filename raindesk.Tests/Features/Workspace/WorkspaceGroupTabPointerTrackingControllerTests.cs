using raindesk.Features.Workspace.Presentation.WinUI;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabPointerTrackingControllerTests
{
    [TestMethod]
    public void DefaultController_CanEndWithoutActiveTracking()
    {
        var controller = new WorkspaceGroupTabPointerTrackingController();

        controller.End();

        Assert.IsFalse(controller.IsTracking);
    }
}
