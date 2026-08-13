using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Application;

internal sealed class LauncherActivationThrottleState
{
    private string? _lastShortcutId;
    private long _lastActivationMilliseconds;

    public bool ShouldSuppress(
        string shortcutId,
        long nowMilliseconds,
        long debounceMilliseconds)
    {
        bool suppress = string.Equals(
                _lastShortcutId,
                shortcutId,
                StringComparison.Ordinal)
            && nowMilliseconds >= _lastActivationMilliseconds
            && nowMilliseconds - _lastActivationMilliseconds <= debounceMilliseconds;
        if (!suppress)
        {
            _lastShortcutId = shortcutId;
            _lastActivationMilliseconds = nowMilliseconds;
        }
        return suppress;
    }
}

public sealed class LauncherActivationService : ILauncherActivationService
{
    private const long DefaultDebounceMilliseconds = 800;
    private readonly ILauncherActivationPort _port;
    private readonly TimeProvider _timeProvider;
    private readonly LauncherActivationThrottleState _throttle = new();

    public LauncherActivationService(
        ILauncherActivationPort port,
        TimeProvider? timeProvider = null)
    {
        _port = port ?? throw new ArgumentNullException(nameof(port));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public LauncherActivationResult Execute(LauncherActivationCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.ShortcutId))
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.InvalidRequest,
                "快捷方式 ID 不能为空。");
        }

        if (command.Mode == LauncherActivationMode.Default
            && _throttle.ShouldSuppress(
                command.ShortcutId,
                _timeProvider.GetUtcNow().ToUnixTimeMilliseconds(),
                DefaultDebounceMilliseconds))
        {
            return LauncherActivationResult.Debounced;
        }

        try
        {
            return _port.Activate(command) ?? LauncherActivationResult.Failed(
                LauncherActivationErrorCode.ActivationFailed,
                "激活适配器没有返回结果。");
        }
        catch (Exception ex)
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.ActivationFailed,
                ex.Message);
        }
    }
}
