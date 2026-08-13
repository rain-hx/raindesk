using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;

namespace raindesk.Tests.Features.Launcher;

[TestClass]
public class LauncherActivationServiceTests
{
    [TestMethod]
    public void Execute_DefaultActivation_DebouncesSameStableIdForEightHundredMilliseconds()
    {
        var port = new StubPort();
        var time = new ManualTimeProvider(DateTimeOffset.UnixEpoch);
        var service = new LauncherActivationService(port, time);
        var command = new LauncherActivationCommand("shortcut-1");

        LauncherActivationResult first = service.Execute(command);
        time.Advance(TimeSpan.FromMilliseconds(500));
        LauncherActivationResult second = service.Execute(command);
        time.Advance(TimeSpan.FromMilliseconds(301));
        LauncherActivationResult third = service.Execute(command);

        Assert.IsTrue(first.Dispatched);
        Assert.IsTrue(second.Suppressed);
        Assert.IsTrue(third.Dispatched);
        Assert.AreEqual(2, port.CallCount);
    }

    [TestMethod]
    public void Execute_ExplicitAdministratorActivation_IsNotDebounced()
    {
        var port = new StubPort();
        var service = new LauncherActivationService(
            port,
            new ManualTimeProvider(DateTimeOffset.UnixEpoch));
        var command = new LauncherActivationCommand(
            "shortcut-1",
            LauncherActivationMode.Administrator);

        service.Execute(command);
        service.Execute(command);

        Assert.AreEqual(2, port.CallCount);
    }

    [TestMethod]
    public void Execute_PortFailure_IsConvertedToStructuredResult()
    {
        var port = new StubPort { Failure = new InvalidOperationException("shell failed") };
        var service = new LauncherActivationService(port);

        LauncherActivationResult result = service.Execute(
            new LauncherActivationCommand("shortcut-1"));

        Assert.IsFalse(result.Success);
        Assert.AreEqual(LauncherActivationErrorCode.ActivationFailed, result.ErrorCode);
        StringAssert.Contains(result.Error!, "shell failed");
    }

    [TestMethod]
    public void GestureCoordinator_RoutesAddFolderSingleAndDoubleActivation()
    {
        var port = new StubPort();
        var service = new LauncherActivationService(port);
        int addPanels = 0;
        var openedFolders = new List<string>();
        var coordinator = new LauncherIconActivationCoordinator<Item>(
            service,
            item => item.Id,
            item => item.IsAdd,
            item => item.IsFolder,
            () => addPanels++,
            item => openedFolders.Add(item.Id));

        bool addHandled = coordinator.HandleTap(
            new Item("add", IsAdd: true),
            true,
            modifierDown: false,
            batchMode: false,
            doubleOpen: false);
        bool folderHandled = coordinator.HandleTap(
            new Item("folder", IsFolder: true),
            true,
            modifierDown: false,
            batchMode: false,
            doubleOpen: false);
        coordinator.HandleTap(
            new Item("single"),
            true,
            modifierDown: false,
            batchMode: false,
            doubleOpen: false);
        bool ignoredSingle = coordinator.HandleTap(
            new Item("double"),
            true,
            modifierDown: false,
            batchMode: false,
            doubleOpen: true);
        bool doubleHandled = coordinator.HandleDoubleTap(
            new Item("double"),
            true,
            batchMode: false,
            doubleOpen: true);

        Assert.IsTrue(addHandled);
        Assert.IsTrue(folderHandled);
        Assert.IsTrue(ignoredSingle);
        Assert.IsTrue(doubleHandled);
        Assert.AreEqual(1, addPanels);
        CollectionAssert.AreEqual(new[] { "folder" }, openedFolders);
        CollectionAssert.AreEqual(new[] { "single", "double" }, port.ShortcutIds.ToArray());
    }

    private sealed record Item(
        string Id,
        bool IsAdd = false,
        bool IsFolder = false);

    private sealed class StubPort : ILauncherActivationPort
    {
        public int CallCount { get; private set; }
        public List<string> ShortcutIds { get; } = new();
        public Exception? Failure { get; init; }

        public LauncherActivationResult Activate(LauncherActivationCommand command)
        {
            CallCount++;
            ShortcutIds.Add(command.ShortcutId);
            if (Failure != null)
            {
                throw Failure;
            }
            return LauncherActivationResult.Activated;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public ManualTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration)
        {
            _now += duration;
        }
    }
}
