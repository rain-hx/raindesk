using rainspace.Features.ImportExport.Contracts;
using rainspace.Features.Plugins.Contracts;

namespace rainspace.Features.ImportExport.Application;

public sealed class PluginImporterRouter : IPluginImporterRouter
{
    private static readonly TimeSpan ImportTimeout = TimeSpan.FromSeconds(30);
    private readonly IPluginContributionGate _gate;
    private readonly TimeSpan _importTimeout;
    private readonly Dictionary<(string PluginId, string ContributionId), Entry> _entries = new();

    public PluginImporterRouter(
        IEnumerable<IPluginImporterContribution> contributions,
        IPluginContributionGate gate,
        TimeSpan? importTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _importTimeout = importTimeout ?? ImportTimeout;
        if (_importTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(importTimeout));
        }

        foreach (IPluginImporterContribution contribution in contributions)
        {
            try
            {
                PluginImporterDescriptor descriptor = contribution.Descriptor;
                if (string.IsNullOrWhiteSpace(descriptor.PluginId)
                    || string.IsNullOrWhiteSpace(descriptor.ContributionId)
                    || string.IsNullOrWhiteSpace(descriptor.DisplayName))
                {
                    continue;
                }

                _entries.TryAdd(
                    (descriptor.PluginId, descriptor.ContributionId),
                    new Entry(descriptor, contribution));
            }
            catch
            {
                // One malformed importer cannot prevent healthy contributions from loading.
            }
        }
    }

    public IReadOnlyList<PluginImporterDescriptor> GetAvailableImporters() =>
        _entries.Values
            .Where(entry => IsEnabled(entry.Descriptor))
            .Select(entry => entry.Descriptor)
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<PluginImportResult> ImportAsync(
        string pluginId,
        string contributionId,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId)
            || string.IsNullOrWhiteSpace(contributionId)
            || string.IsNullOrWhiteSpace(sourcePath))
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.InvalidRequest,
                "插件、贡献点和导入路径不能为空。");
        }

        if (!_entries.TryGetValue((pluginId, contributionId), out Entry? entry))
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.NotFound,
                "插件导入器不存在。");
        }

        if (!IsEnabled(entry.Descriptor))
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.Disabled,
                "插件导入器未启用或没有所需 capability。");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_importTimeout);
        try
        {
            PluginImportResult? result = await entry.Contribution
                .ImportAsync(sourcePath, timeoutSource.Token)
                .ConfigureAwait(false);
            return result ?? PluginImportResult.Failed(
                PluginImportErrorCode.ImportFailed,
                "插件导入器没有返回结果。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.Cancelled,
                "插件导入已取消。");
        }
        catch (OperationCanceledException)
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.Timeout,
                "插件导入执行超时。");
        }
        catch (Exception ex)
        {
            return PluginImportResult.Failed(
                PluginImportErrorCode.ImportFailed,
                ex.Message);
        }
    }

    private bool IsEnabled(PluginImporterDescriptor descriptor)
    {
        try
        {
            return _gate.IsEnabled(
                descriptor.PluginId,
                descriptor.ContributionId,
                PluginContributionKind.Importer);
        }
        catch
        {
            return false;
        }
    }

    private sealed record Entry(
        PluginImporterDescriptor Descriptor,
        IPluginImporterContribution Contribution);
}
