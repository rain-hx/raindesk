using rainspace.Features.Workspace.Presentation.WinUI;
using rainspace.Features.Workspace.Presentation;
using Microsoft.UI.Xaml.Input;

namespace rainspace.Tests.Features.Workspace;

[TestClass]
public class WorkspaceGroupTabChromeControllerTests
{
    [TestMethod]
    public void Controller_CanBeConstructedWithoutUiState()
    {
        var controller = new WorkspaceGroupTabChromeController();

        Assert.IsNotNull(controller);
    }

    [TestMethod]
    public void ContainerController_CanBeConstructedFromExplicitUiCallbacks()
    {
        PointerEventHandler pointerHandler = static (_, _) => { };

        var controller = new WorkspaceGroupTabContainerController(
            new WorkspaceGroupTabChromeController(),
            static _ => { },
            WorkspaceGroupTabWidthMetrics.Default,
            pointerHandler,
            pointerHandler,
            pointerHandler,
            pointerHandler,
            static () => null,
            static () => null,
            static _ => false,
            static () => { });

        Assert.IsNotNull(controller);
    }

    [TestMethod]
    public void DragCoordinator_CanBeConstructedWithoutGlobalWindowState()
    {
        IList<string> tabs = new List<string> { "all", "one" };
        var coordinator = new WorkspaceGroupTabDragCoordinator<string>(
            static () => null,
            static () => null,
            static () => null,
            static () => null,
            () => tabs,
            static () => true,
            static _ => null,
            static tab => tab == "all",
            static () => { },
            WorkspaceGroupTabWidthMetrics.Default);

        coordinator.Abort();

        Assert.IsFalse(coordinator.ConsumeSuppressedClick());
    }
}
