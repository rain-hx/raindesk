using rainspace.Features.Plugins.Application;
using rainspace.Features.Plugins.Contracts;

namespace rainspace.Tests.Features.Plugins;

[TestClass]
public class PluginCatalogTests
{
    [TestMethod]
    public void Validator_RejectsExternalPluginAndCapabilityMismatchByDefault()
    {
        var validator = new PluginManifestValidator();
        PluginManifest manifest = Manifest(
            "external.plugin",
            isBuiltIn: false,
            capabilities: Array.Empty<PluginCapability>(),
            contributions: new[]
            {
                new PluginContributionDescriptor(
                    "search",
                    PluginContributionKind.SearchProvider,
                    "Search")
            },
            entryPoint: "../plugin.dll");

        PluginValidationResult result = validator.Validate(manifest);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Errors.Any(error => error.Contains("仅允许", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("安全相对路径", StringComparison.Ordinal)));
        Assert.IsTrue(result.Errors.Any(error => error.Contains("capability Search", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task Initialize_FailingPluginDoesNotDisableHealthyPlugin()
    {
        var healthy = new FakePlugin(Manifest("healthy.plugin"));
        var failing = new FakePlugin(Manifest("failing.plugin"))
        {
            StartFailure = new InvalidOperationException("boom")
        };
        var catalog = new PluginCatalog(
            new IBuiltInPlugin[] { healthy, failing },
            new PluginManifestValidator());

        await catalog.InitializeAsync();

        IReadOnlyList<PluginDescriptor> plugins = catalog.GetPlugins();
        Assert.AreEqual(PluginState.Enabled, plugins.Single(p => p.Manifest.Id == "healthy.plugin").State);
        Assert.AreEqual(PluginState.Failed, plugins.Single(p => p.Manifest.Id == "failing.plugin").State);
        Assert.AreEqual(1, healthy.StartCount);
        Assert.AreEqual(1, failing.StartCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task DisableAndEnable_ControlsContributionGate()
    {
        var plugin = new FakePlugin(Manifest("search.plugin"));
        var catalog = new PluginCatalog(
            new[] { plugin },
            new PluginManifestValidator());
        await catalog.InitializeAsync();

        Assert.IsTrue(catalog.IsEnabled(
            "search.plugin",
            "search",
            PluginContributionKind.SearchProvider));
        Assert.IsFalse(catalog.IsEnabled(
            "search.plugin",
            "search",
            PluginContributionKind.SettingsSection));

        PluginDescriptor? disabled = await catalog.DisableAsync("search.plugin");

        Assert.AreEqual(PluginState.Disabled, disabled?.State);
        Assert.IsFalse(catalog.IsEnabled(
            "search.plugin",
            "search",
            PluginContributionKind.SearchProvider));
        Assert.AreEqual(1, plugin.StopCount);

        PluginDescriptor? enabled = await catalog.EnableAsync("search.plugin");

        Assert.AreEqual(PluginState.Enabled, enabled?.State);
        Assert.IsTrue(catalog.IsEnabled(
            "search.plugin",
            "search",
            PluginContributionKind.SearchProvider));
        Assert.AreEqual(2, plugin.StartCount);
    }

    [TestMethod]
    public async global::System.Threading.Tasks.Task InvalidManifest_IsNeverStarted()
    {
        var plugin = new FakePlugin(Manifest("INVALID ID"));
        var catalog = new PluginCatalog(
            new[] { plugin },
            new PluginManifestValidator());

        await catalog.InitializeAsync();

        PluginDescriptor descriptor = catalog.GetPlugins().Single();
        Assert.AreEqual(PluginState.Failed, descriptor.State);
        Assert.AreEqual(0, plugin.StartCount);
    }

    private static PluginManifest Manifest(
        string id,
        bool isBuiltIn = true,
        IReadOnlyList<PluginCapability>? capabilities = null,
        IReadOnlyList<PluginContributionDescriptor>? contributions = null,
        string? entryPoint = null) => new(
            id,
            id,
            "1.0.0",
            PluginApi.CurrentVersion,
            isBuiltIn,
            capabilities ?? new[] { PluginCapability.Search },
            contributions ?? new[]
            {
                new PluginContributionDescriptor(
                    "search",
                    PluginContributionKind.SearchProvider,
                    "Search")
            },
            entryPoint);

    private sealed class FakePlugin : IBuiltInPlugin
    {
        public FakePlugin(PluginManifest manifest)
        {
            Manifest = manifest;
        }

        public PluginManifest Manifest { get; }
        public Exception? StartFailure { get; init; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }

        public global::System.Threading.Tasks.Task StartAsync(
            CancellationToken cancellationToken = default)
        {
            StartCount++;
            return StartFailure == null
                ? global::System.Threading.Tasks.Task.CompletedTask
                : global::System.Threading.Tasks.Task.FromException(StartFailure);
        }

        public global::System.Threading.Tasks.Task StopAsync(
            CancellationToken cancellationToken = default)
        {
            StopCount++;
            return global::System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
