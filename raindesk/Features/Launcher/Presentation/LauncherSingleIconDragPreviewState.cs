namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// 单图标拖拽的稳定目标状态；隔离目标去抖与页面计时来源。
/// </summary>
internal sealed class LauncherSingleIconDragPreviewState
{
    public int TargetIndex { get; private set; } = -1;

    public int PendingTargetIndex { get; private set; } = -1;

    public long PendingSinceTick { get; private set; }

    public void Begin(int sourceIndex)
    {
        TargetIndex = sourceIndex;
        ClearPending();
    }

    public bool TryActivateTarget(
        int targetIndex,
        long nowTick,
        int activationDelayMilliseconds)
    {
        if (TargetIndex == targetIndex)
        {
            ClearPending();
            return true;
        }

        if (PendingTargetIndex != targetIndex)
        {
            PendingTargetIndex = targetIndex;
            PendingSinceTick = nowTick;
            return false;
        }

        long elapsed = nowTick - PendingSinceTick;
        if (elapsed < Math.Max(0, activationDelayMilliseconds))
        {
            return false;
        }

        ClearPending();
        return true;
    }

    public void CommitTarget(int targetIndex)
    {
        TargetIndex = targetIndex;
        ClearPending();
    }

    public void ClearPending()
    {
        PendingTargetIndex = -1;
        PendingSinceTick = 0;
    }

    public void Reset()
    {
        TargetIndex = -1;
        ClearPending();
    }
}
