using raindesk.Features.Plugins.Contracts;

namespace raindesk.Features.Plugins.Application;

public sealed class PluginCatalog : IPluginCatalog
{
    private static readonly TimeSpan LifecycleTimeout = TimeSpan.FromSeconds(5);
    private readonly IPluginManifestValidator _validator;
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private bool _initialized;

    public PluginCatalog(
        IEnumerable<IBuiltInPlugin> plugins,
        IPluginManifestValidator validator)
    {
        ArgumentNullException.ThrowIfNull(plugins);
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));

        foreach (IBuiltInPlugin plugin in plugins)
        {
            PluginManifest manifest = plugin.Manifest;
            PluginValidationResult validation = _validator.Validate(manifest);
            if (_entries.ContainsKey(manifest.Id))
            {
                _entries[manifest.Id] = new Entry(
                    plugin,
                    new PluginDescriptor(manifest, PluginState.Failed, "插件 ID 重复。"));
                continue;
            }

            _entries.Add(
                manifest.Id,
                new Entry(
                    plugin,
                    validation.Success
                        ? new PluginDescriptor(manifest, PluginState.Discovered)
                        : new PluginDescriptor(
                            manifest,
                            PluginState.Failed,
                            string.Join(" ", validation.Errors))));
        }
    }

    public IReadOnlyList<PluginDescriptor> GetPlugins() => _entries.Values
        .Select(entry => entry.Descriptor)
        .OrderBy(descriptor => descriptor.Manifest.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public bool IsEnabled(
        string pluginId,
        string contributionId,
        PluginContributionKind contributionKind)
    {
        if (!_entries.TryGetValue(pluginId, out Entry? entry)
            || entry.Descriptor.State != PluginState.Enabled)
        {
            return false;
        }

        return entry.Descriptor.Manifest.Contributions.Any(contribution =>
            string.Equals(contribution.Id, contributionId, StringComparison.Ordinal)
            && contribution.Kind == contributionKind);
    }

    public async global::System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            foreach (Entry entry in _entries.Values.Where(entry =>
                         entry.Descriptor.State == PluginState.Discovered))
            {
                await StartEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<PluginDescriptor?> EnableAsync(
        string pluginId,
        CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_entries.TryGetValue(pluginId, out Entry? entry)
                || entry.Descriptor.State == PluginState.Failed)
            {
                return entry?.Descriptor;
            }

            if (entry.Descriptor.State != PluginState.Enabled)
            {
                await StartEntryAsync(entry, cancellationToken).ConfigureAwait(false);
            }
            return entry.Descriptor;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<PluginDescriptor?> DisableAsync(
        string pluginId,
        CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_entries.TryGetValue(pluginId, out Entry? entry))
            {
                return null;
            }
            if (entry.Descriptor.State != PluginState.Enabled)
            {
                return entry.Descriptor;
            }

            try
            {
                await RunWithTimeoutAsync(entry.Plugin.StopAsync, cancellationToken)
                    .ConfigureAwait(false);
                entry.Descriptor = entry.Descriptor with
                {
                    State = PluginState.Disabled,
                    Error = null
                };
            }
            catch (Exception ex)
            {
                entry.Descriptor = entry.Descriptor with
                {
                    State = PluginState.Failed,
                    Error = ex.Message
                };
            }
            return entry.Descriptor;
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private static async global::System.Threading.Tasks.Task StartEntryAsync(
        Entry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await RunWithTimeoutAsync(entry.Plugin.StartAsync, cancellationToken)
                .ConfigureAwait(false);
            entry.Descriptor = entry.Descriptor with
            {
                State = PluginState.Enabled,
                Error = null
            };
        }
        catch (Exception ex)
        {
            entry.Descriptor = entry.Descriptor with
            {
                State = PluginState.Failed,
                Error = ex.Message
            };
        }
    }

    private static async global::System.Threading.Tasks.Task RunWithTimeoutAsync(
        Func<CancellationToken, global::System.Threading.Tasks.Task> operation,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(LifecycleTimeout);
        await operation(timeoutSource.Token).ConfigureAwait(false);
    }

    private sealed class Entry
    {
        public Entry(IBuiltInPlugin plugin, PluginDescriptor descriptor)
        {
            Plugin = plugin;
            Descriptor = descriptor;
        }

        public IBuiltInPlugin Plugin { get; }

        public PluginDescriptor Descriptor { get; set; }
    }
}
