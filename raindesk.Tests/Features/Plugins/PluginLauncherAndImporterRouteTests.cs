using raindesk.Features.ImportExport.Application;
using raindesk.Features.ImportExport.Contracts;
using raindesk.Features.Launcher.Application;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Plugins.Contracts;
using ThreadingTask = System.Threading.Tasks.Task;

namespace raindesk.Tests.Features.Plugins;

[TestClass]
public class PluginLauncherAndImporterRouteTests
{
    [TestMethod]
    public async ThreadingTask LauncherRouter_EnabledContribution_IsListedAndExecuted()
    {
        var contribution = new StubLauncherCommand();
        var router = new LauncherPluginCommandRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.LauncherCommand));

        IReadOnlyList<LauncherPluginCommandDescriptor> commands = router.GetAvailableCommands();
        LauncherPluginCommandResult result = await router.ExecuteAsync(
            "plugin",
            "launch",
            new LauncherPluginCommandContext(new Dictionary<string, string?>
            {
                ["target"] = "demo"
            }));

        Assert.AreEqual(1, commands.Count);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, contribution.ExecuteCount);
        Assert.AreEqual("demo", contribution.LastTarget);
    }

    [TestMethod]
    public async ThreadingTask LauncherRouter_DisabledContribution_IsNotInvoked()
    {
        var contribution = new StubLauncherCommand();
        var router = new LauncherPluginCommandRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.Importer));

        LauncherPluginCommandResult result = await router.ExecuteAsync(
            "plugin",
            "launch",
            new LauncherPluginCommandContext(new Dictionary<string, string?>()));

        Assert.AreEqual(0, router.GetAvailableCommands().Count);
        Assert.AreEqual(LauncherPluginCommandErrorCode.Disabled, result.ErrorCode);
        Assert.AreEqual(0, contribution.ExecuteCount);
    }

    [TestMethod]
    public async ThreadingTask LauncherRouter_Timeout_IsConvertedToStructuredFailure()
    {
        var contribution = new StubLauncherCommand { WaitUntilCancelled = true };
        var router = new LauncherPluginCommandRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.LauncherCommand),
            TimeSpan.FromMilliseconds(20));

        LauncherPluginCommandResult result = await router.ExecuteAsync(
            "plugin",
            "launch",
            new LauncherPluginCommandContext(new Dictionary<string, string?>()));

        Assert.AreEqual(LauncherPluginCommandErrorCode.Timeout, result.ErrorCode);
    }

    [TestMethod]
    public async ThreadingTask ImporterRouter_EnabledContribution_IsListedAndExecuted()
    {
        var contribution = new StubImporter();
        var router = new PluginImporterRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.Importer));

        IReadOnlyList<PluginImporterDescriptor> importers = router.GetAvailableImporters();
        PluginImportResult result = await router.ImportAsync(
            "plugin",
            "import",
            "sample.demo");

        Assert.AreEqual(1, importers.Count);
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, contribution.ImportCount);
        Assert.AreEqual("sample.demo", contribution.LastPath);
    }

    [TestMethod]
    public async ThreadingTask ImporterRouter_DisabledContribution_IsNotInvoked()
    {
        var contribution = new StubImporter();
        var router = new PluginImporterRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.LauncherCommand));

        PluginImportResult result = await router.ImportAsync(
            "plugin",
            "import",
            "sample.demo");

        Assert.AreEqual(0, router.GetAvailableImporters().Count);
        Assert.AreEqual(PluginImportErrorCode.Disabled, result.ErrorCode);
        Assert.AreEqual(0, contribution.ImportCount);
    }

    [TestMethod]
    public async ThreadingTask ImporterRouter_Exception_IsConvertedToStructuredFailure()
    {
        var contribution = new StubImporter
        {
            Failure = new InvalidOperationException("broken importer")
        };
        var router = new PluginImporterRouter(
            new[] { contribution },
            new KindGate(PluginContributionKind.Importer));

        PluginImportResult result = await router.ImportAsync(
            "plugin",
            "import",
            "sample.demo");

        Assert.AreEqual(PluginImportErrorCode.ImportFailed, result.ErrorCode);
        StringAssert.Contains(result.Error, "broken importer");
    }

    private sealed class KindGate : IPluginContributionGate
    {
        private readonly PluginContributionKind _enabledKind;

        public KindGate(PluginContributionKind enabledKind)
        {
            _enabledKind = enabledKind;
        }

        public bool IsEnabled(
            string pluginId,
            string contributionId,
            PluginContributionKind contributionKind) =>
            pluginId == "plugin" && contributionKind == _enabledKind;
    }

    private sealed class StubLauncherCommand : ILauncherPluginCommandContribution
    {
        public LauncherPluginCommandDescriptor Descriptor { get; } =
            new("plugin", "launch", "Launch demo");

        public int ExecuteCount { get; private set; }
        public string? LastTarget { get; private set; }
        public bool WaitUntilCancelled { get; init; }

        public async global::System.Threading.Tasks.Task<LauncherPluginCommandResult> ExecuteAsync(
            LauncherPluginCommandContext context,
            CancellationToken cancellationToken = default)
        {
            ExecuteCount++;
            context.Arguments.TryGetValue("target", out string? target);
            LastTarget = target;
            if (WaitUntilCancelled)
            {
                await ThreadingTask.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return LauncherPluginCommandResult.Succeeded;
        }
    }

    private sealed class StubImporter : IPluginImporterContribution
    {
        public PluginImporterDescriptor Descriptor { get; } =
            new("plugin", "import", "Demo importer", new[] { ".demo" });

        public int ImportCount { get; private set; }
        public string? LastPath { get; private set; }
        public Exception? Failure { get; init; }

        public global::System.Threading.Tasks.Task<PluginImportResult> ImportAsync(
            string sourcePath,
            CancellationToken cancellationToken = default)
        {
            ImportCount++;
            LastPath = sourcePath;
            return Failure == null
                ? ThreadingTask.FromResult(PluginImportResult.Succeeded)
                : ThreadingTask.FromException<PluginImportResult>(Failure);
        }
    }
}
