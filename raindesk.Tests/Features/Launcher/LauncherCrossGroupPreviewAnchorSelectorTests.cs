using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherCrossGroupPreviewAnchorSelectorTests
{
    [TestMethod]
    public void Select_PrefersRealizedPlaceholderOverDraggedItem()
    {
        var placeholder = new object();
        var dragged = new object();

        LauncherCrossGroupPreviewAnchor<object> anchor =
            LauncherCrossGroupPreviewAnchorSelector.Select(
                new[] { dragged, placeholder },
                new[] { placeholder },
                new[] { dragged });

        Assert.AreSame(placeholder, anchor.Item);
        Assert.AreEqual(LauncherCrossGroupPreviewAnchorKind.Placeholder, anchor.Kind);
    }

    [TestMethod]
    public void Select_UsesFirstDraggedItemPresentInActiveHost()
    {
        var missingPlaceholder = new object();
        var missingDragged = new object();
        var realizedDragged = new object();

        LauncherCrossGroupPreviewAnchor<object> anchor =
            LauncherCrossGroupPreviewAnchorSelector.Select(
                new[] { realizedDragged },
                new[] { missingPlaceholder },
                new[] { missingDragged, realizedDragged });

        Assert.AreSame(realizedDragged, anchor.Item);
        Assert.AreEqual(LauncherCrossGroupPreviewAnchorKind.DraggedItem, anchor.Kind);
    }

    [TestMethod]
    public void Select_ReturnsNoneWhenNoCandidateExistsInHost()
    {
        LauncherCrossGroupPreviewAnchor<object> anchor =
            LauncherCrossGroupPreviewAnchorSelector.Select(
                Array.Empty<object>(),
                new[] { new object() },
                new[] { new object() });

        Assert.IsNull(anchor.Item);
        Assert.AreEqual(LauncherCrossGroupPreviewAnchorKind.None, anchor.Kind);
    }
}
