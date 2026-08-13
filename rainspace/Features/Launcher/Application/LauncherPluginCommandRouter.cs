using rainspace.Features.Launcher.Contracts;
using rainspace.Features.Plugins.Contracts;

namespace rainspace.Features.Launcher.Application;

public sealed class LauncherPluginCommandRouter : ILauncherPluginCommandRouter
{
    private static readonly TimeSpan ExecutionTimeout = TimeSpan.FromSeconds(5);
    private readonly IPluginContributionGate _gate;
    private readonly TimeSpan _executionTimeout;
    private readonly Dictionary<(string PluginId, string ContributionId), Entry> _entries = new();

    public LauncherPluginCommandRouter(
        IEnumerable<ILauncherPluginCommandContribution> contributions,
        IPluginContributionGate gate,
        TimeSpan? executionTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(contributions);
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _executionTimeout = executionTimeout ?? ExecutionTimeout;
        if (_executionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(executionTimeout));
        }

        foreach (ILauncherPluginCommandContribution contribution in contributions)
        {
            try
            {
                LauncherPluginCommandDescriptor descriptor = contribution.Descriptor;
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
                // One malformed contribution cannot prevent other commands from being routed.
            }
        }
    }

    public IReadOnlyList<LauncherPluginCommandDescriptor> GetAvailableCommands() =>
        _entries.Values
            .Where(entry => IsEnabled(entry.Descriptor))
            .Select(entry => entry.Descriptor)
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public async Task<LauncherPluginCommandResult> ExecuteAsync(
        string pluginId,
        string contributionId,
        LauncherPluginCommandContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId)
            || string.IsNullOrWhiteSpace(contributionId)
            || context == null)
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.InvalidRequest,
                "插件、贡献点和命令上下文不能为空。");
        }

        if (!_entries.TryGetValue((pluginId, contributionId), out Entry? entry))
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.NotFound,
                "Launcher 插件命令不存在。");
        }

        if (!IsEnabled(entry.Descriptor))
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.Disabled,
                "Launcher 插件命令未启用或没有所需 capability。");
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(_executionTimeout);
        try
        {
            LauncherPluginCommandResult? result = await entry.Contribution
                .ExecuteAsync(context, timeoutSource.Token)
                .ConfigureAwait(false);
            return result ?? LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.ExecutionFailed,
                "Launcher 插件命令没有返回结果。");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.Cancelled,
                "Launcher 插件命令已取消。");
        }
        catch (OperationCanceledException)
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.Timeout,
                "Launcher 插件命令执行超时。");
        }
        catch (Exception ex)
        {
            return LauncherPluginCommandResult.Failed(
                LauncherPluginCommandErrorCode.ExecutionFailed,
                ex.Message);
        }
    }

    private bool IsEnabled(LauncherPluginCommandDescriptor descriptor)
    {
        try
        {
            return _gate.IsEnabled(
                descriptor.PluginId,
                descriptor.ContributionId,
                PluginContributionKind.LauncherCommand);
        }
        catch
        {
            return false;
        }
    }

    private sealed record Entry(
        LauncherPluginCommandDescriptor Descriptor,
        ILauncherPluginCommandContribution Contribution);
}
