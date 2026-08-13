using raindesk.Features.Workspace.Presentation.WinUI;

namespace raindesk.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabDragPreviewControllerTests
{
    [TestMethod]
    public void DefaultController_CanEndWithoutPreview()
    {
        var controller = new WorkspaceGroupTabDragPreviewController();

        controller.End();

        Assert.IsFalse(controller.HasPreview);
    }
}
