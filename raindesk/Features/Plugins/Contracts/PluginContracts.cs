namespace raindesk.Features.Plugins.Contracts;

public static class PluginApi
{
    public const int CurrentVersion = 1;
}

public enum PluginCapability
{
    Search,
    Settings,
    LauncherCommands,
    Importers,
    Sidecar
}

public enum PluginContributionKind
{
    SearchProvider,
    SettingsSection,
    LauncherCommand,
    Importer
}

public enum PluginState
{
    Discovered,
    Enabled,
    Disabled,
    Failed
}

public sealed record PluginContributionDescriptor(
    string Id,
    PluginContributionKind Kind,
    string DisplayName);

public sealed record PluginManifest(
    string Id,
    string Name,
    string Version,
    int ApiVersion,
    bool IsBuiltIn,
    IReadOnlyList<PluginCapability> Capabilities,
    IReadOnlyList<PluginContributionDescriptor> Contributions,
    string? EntryPoint = null);

public sealed record PluginDescriptor(
    PluginManifest Manifest,
    PluginState State,
    string? Error = null);

public sealed record PluginValidationResult(
    bool Success,
    IReadOnlyList<string> Errors)
{
    public static PluginValidationResult Valid { get; } = new(true, Array.Empty<string>());
}

public interface IPluginManifestValidator
{
    PluginValidationResult Validate(PluginManifest manifest);
}

/// <summary>仅由 composition root 显式注册；不扫描或反射加载任意程序集。</summary>
public interface IBuiltInPlugin
{
    PluginManifest Manifest { get; }

    global::System.Threading.Tasks.Task StartAsync(CancellationToken cancellationToken = default);

    global::System.Threading.Tasks.Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IPluginContributionGate
{
    bool IsEnabled(
        string pluginId,
        string contributionId,
        PluginContributionKind contributionKind);
}

public interface IPluginCatalog : IPluginContributionGate
{
    IReadOnlyList<PluginDescriptor> GetPlugins();

    global::System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken = default);

    Task<PluginDescriptor?> EnableAsync(
        string pluginId,
        CancellationToken cancellationToken = default);

    Task<PluginDescriptor?> DisableAsync(
        string pluginId,
        CancellationToken cancellationToken = default);
}
