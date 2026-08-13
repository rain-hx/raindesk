using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherInlineSystemItemsCoordinatorTests
{
    [TestMethod]
    public void PanelState_PreventsOverlappingInlineModesUntilClosed()
    {
        var state = new LauncherInlineAddPanelState();

        bool openedUrl = state.TryOpen(LauncherInlinePanelMode.Url);
        bool openedSystemWhileBusy = state.TryOpen(LauncherInlinePanelMode.SystemItems);
        state.Close();
        bool openedSystem = state.TryOpen(LauncherInlinePanelMode.SystemItems);

        Assert.IsTrue(openedUrl);
        Assert.IsFalse(openedSystemWhileBusy);
        Assert.IsTrue(openedSystem);
        Assert.AreEqual(LauncherInlinePanelMode.SystemItems, state.Mode);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task SelectAsync_CachesSourceAndFiltersCaseInsensitively()
    {
        int loadCount = 0;
        var updates = new List<LauncherInlineSystemItemsUpdate<string>>();
        using var coordinator = new LauncherInlineSystemItemsCoordinator<string>(
            _ =>
            {
                loadCount++;
                return global::System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(
                    new[] { "Calculator", "Settings" });
            },
            _ => global::System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(
                Array.Empty<string>()),
            item => item,
            TimeSpan.Zero);
        coordinator.Updated += updates.Add;

        await coordinator.SelectAsync(LauncherInlineSystemItemsKind.BuiltIn, "calc");
        await coordinator.SelectAsync(LauncherInlineSystemItemsKind.BuiltIn, "SET");

        Assert.AreEqual(1, loadCount);
        CollectionAssert.AreEqual(new[] { "Calculator" }, updates[^2].Items.ToArray());
        CollectionAssert.AreEqual(new[] { "Settings" }, updates[^1].Items.ToArray());
        Assert.IsFalse(updates[^1].IsLoading);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task SelectAsync_LatePreviousLoadCannotReplaceNewKind()
    {
        var startMenuCompletion =
            new TaskCompletionSource<IReadOnlyList<string>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        var updates = new List<LauncherInlineSystemItemsUpdate<string>>();
        using var coordinator = new LauncherInlineSystemItemsCoordinator<string>(
            _ => global::System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(
                new[] { "System" }),
            _ => startMenuCompletion.Task,
            item => item,
            TimeSpan.Zero);
        coordinator.Updated += updates.Add;

        global::System.Threading.Tasks.Task oldLoad = coordinator.SelectAsync(
            LauncherInlineSystemItemsKind.StartMenu);
        await coordinator.SelectAsync(LauncherInlineSystemItemsKind.BuiltIn);
        startMenuCompletion.SetResult(new[] { "Late start item" });
        await oldLoad;

        LauncherInlineSystemItemsUpdate<string> last = updates[^1];
        Assert.AreEqual(LauncherInlineSystemItemsKind.BuiltIn, last.Kind);
        CollectionAssert.AreEqual(new[] { "System" }, last.Items.ToArray());
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task FilterAsync_CancelsPreviousDebounceRequest()
    {
        var updates = new List<LauncherInlineSystemItemsUpdate<string>>();
        using var coordinator = new LauncherInlineSystemItemsCoordinator<string>(
            _ => global::System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(
                Array.Empty<string>()),
            _ => global::System.Threading.Tasks.Task.FromResult<IReadOnlyList<string>>(
                new[] { "Alpha", "Banana" }),
            item => item,
            TimeSpan.FromMilliseconds(10));
        coordinator.Updated += updates.Add;
        await coordinator.SelectAsync(LauncherInlineSystemItemsKind.StartMenu);
        updates.Clear();

        global::System.Threading.Tasks.Task first = coordinator.FilterAsync("alpha");
        global::System.Threading.Tasks.Task second = coordinator.FilterAsync("banana");
        await global::System.Threading.Tasks.Task.WhenAll(first, second);

        Assert.AreEqual(1, updates.Count);
        CollectionAssert.AreEqual(new[] { "Banana" }, updates[0].Items.ToArray());
    }
}
