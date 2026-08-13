namespace raindesk.Features.Launcher.Presentation;

internal readonly record struct LauncherFolderDropIntentUpdate<T>(
    T? PreviousActiveTarget,
    T? ActiveTarget,
    bool IsPending)
    where T : class
{
    public bool ActiveTargetChanged =>
        !ReferenceEquals(PreviousActiveTarget, ActiveTarget);
}

/// <summary>
/// 文件夹/创建文件夹拖放的悬停激活状态机，不依赖 UI、时钟或具体图标模型。
/// </summary>
internal sealed class LauncherFolderDropIntentState<T>
    where T : class
{
    public T? ActiveTarget { get; private set; }

    public T? LastValidTarget { get; private set; }

    public T? PendingTarget { get; private set; }

    public long PendingSinceTick { get; private set; }

    public LauncherFolderDropIntentUpdate<T> EvaluateCandidate(
        T? candidate,
        bool isValid,
        long nowTick,
        int activationDelayMilliseconds)
    {
        T? previous = ActiveTarget;
        if (candidate == null || !isValid)
        {
            ActiveTarget = null;
            ClearPending();
            return new LauncherFolderDropIntentUpdate<T>(previous, null, false);
        }

        if (ReferenceEquals(ActiveTarget, candidate))
        {
            ClearPending();
            return new LauncherFolderDropIntentUpdate<T>(previous, ActiveTarget, false);
        }

        if (!ReferenceEquals(PendingTarget, candidate))
        {
            PendingTarget = candidate;
            PendingSinceTick = nowTick;
            ActiveTarget = null;
            return new LauncherFolderDropIntentUpdate<T>(previous, null, true);
        }

        long elapsed = nowTick - PendingSinceTick;
        if (elapsed < Math.Max(0, activationDelayMilliseconds))
        {
            ActiveTarget = null;
            return new LauncherFolderDropIntentUpdate<T>(previous, null, true);
        }

        ActiveTarget = candidate;
        LastValidTarget = candidate;
        ClearPending();
        return new LauncherFolderDropIntentUpdate<T>(previous, ActiveTarget, false);
    }

    public LauncherFolderDropIntentUpdate<T> SetActive(T? target, bool isValid)
    {
        T? previous = ActiveTarget;
        ActiveTarget = target != null && isValid ? target : null;
        if (ActiveTarget != null)
        {
            LastValidTarget = ActiveTarget;
        }

        ClearPending();
        return new LauncherFolderDropIntentUpdate<T>(previous, ActiveTarget, false);
    }

    public void ClearPending()
    {
        PendingTarget = null;
        PendingSinceTick = 0;
    }

    public void Reset()
    {
        ActiveTarget = null;
        LastValidTarget = null;
        ClearPending();
    }
}
