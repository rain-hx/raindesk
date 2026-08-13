namespace raindesk.Features.Launcher.Presentation;

internal readonly record struct LauncherGroupTabHoverChange<T>(
    T? PreviousTarget,
    T? CurrentTarget)
    where T : class
{
    public bool Changed => !ReferenceEquals(PreviousTarget, CurrentTarget);
}

/// <summary>
/// 分组标签拖拽悬停的唯一活动目标，不依赖分组模型或 WinUI。
/// </summary>
internal sealed class LauncherGroupTabHoverState<T>
    where T : class
{
    public T? Target { get; private set; }

    public LauncherGroupTabHoverChange<T> SetTarget(T? target)
    {
        T? previous = Target;
        Target = target;
        return new LauncherGroupTabHoverChange<T>(previous, Target);
    }

    public LauncherGroupTabHoverChange<T> Clear() => SetTarget(null);
}
