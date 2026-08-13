using rainspace.Features.Workspace.Presentation.WinUI;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabDeleteAnimationControllerTests
{
    [TestMethod]
    public void Controller_CanBeCreatedWithoutWindowState()
    {
        var controller = new WorkspaceGroupTabDeleteAnimationController();

        Assert.IsNotNull(controller);
    }
}
