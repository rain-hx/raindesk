using raindesk.Features.Plugins.Contracts;

namespace raindesk.Features.Plugins.Infrastructure;

public sealed class StaticBuiltInPlugin : IBuiltInPlugin
{
    public StaticBuiltInPlugin(PluginManifest manifest)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
    }

    public PluginManifest Manifest { get; }

    public global::System.Threading.Tasks.Task StartAsync(
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.CompletedTask;

    public global::System.Threading.Tasks.Task StopAsync(
        CancellationToken cancellationToken = default) =>
        global::System.Threading.Tasks.Task.CompletedTask;
}
