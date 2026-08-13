namespace rainspace.Features.Workspace.Presentation;

internal readonly record struct WorkspaceGroupTabDragActivation(
    bool IsActive,
    bool ActivatedNow);

internal readonly record struct WorkspaceGroupTabDragCompletion(
    bool Moved,
    int FinalIndex);

/// <summary>
/// Workspace 分组标签手动重排的纯会话状态。
/// </summary>
internal sealed class WorkspaceGroupTabDragSessionState<TMenu>
    where TMenu : class
{
    private bool _savedReorderEnabled = true;
    private bool _suppressNextClick;

    public bool IsReorderEnabled { get; private set; } = true;

    public TMenu? Menu { get; private set; }

    public int OriginalIndex { get; private set; } = -1;

    public int CurrentIndex { get; private set; } = -1;

    public int PendingTargetIndex { get; private set; } = -1;

    public double StartX { get; private set; }

    public double StartY { get; private set; }

    public bool IsActive { get; private set; }

    public bool HasMoved { get; private set; }

    public bool HasSession => Menu != null;

    public bool WillCommitMove =>
        HasMoved && OriginalIndex >= 1 && CurrentIndex >= 1 && CurrentIndex != OriginalIndex;

    public void SetReorderEnabled(bool enabled) => IsReorderEnabled = enabled;

    public void SuspendReorder()
    {
        _savedReorderEnabled = IsReorderEnabled;
        IsReorderEnabled = false;
    }

    public void RestoreReorder(bool allowed)
    {
        IsReorderEnabled = _savedReorderEnabled && allowed;
    }

    public void Begin(TMenu menu, int sourceIndex, double startX, double startY)
    {
        ArgumentNullException.ThrowIfNull(menu);
        Menu = menu;
        OriginalIndex = sourceIndex;
        CurrentIndex = sourceIndex;
        PendingTargetIndex = sourceIndex;
        StartX = startX;
        StartY = startY;
        IsActive = false;
        HasMoved = false;
    }

    public WorkspaceGroupTabDragActivation TryActivate(
        double currentX,
        double currentY,
        double activationThreshold)
    {
        if (!HasSession)
        {
            return new WorkspaceGroupTabDragActivation(false, false);
        }

        if (IsActive)
        {
            return new WorkspaceGroupTabDragActivation(true, false);
        }

        double dx = currentX - StartX;
        double dy = currentY - StartY;
        if (Math.Sqrt(dx * dx + dy * dy) < Math.Max(0d, activationThreshold))
        {
            return new WorkspaceGroupTabDragActivation(false, false);
        }

        IsActive = true;
        return new WorkspaceGroupTabDragActivation(true, true);
    }

    public bool TrySetPendingTarget(int targetIndex)
    {
        if (targetIndex == PendingTargetIndex)
        {
            return false;
        }

        PendingTargetIndex = targetIndex;
        return true;
    }

    public void CommitLiveMove(int targetIndex)
    {
        if (targetIndex == CurrentIndex)
        {
            return;
        }

        CurrentIndex = targetIndex;
        HasMoved = true;
    }

    public WorkspaceGroupTabDragCompletion Complete()
    {
        bool moved = WillCommitMove;
        int finalIndex = moved ? CurrentIndex : OriginalIndex;
        if (moved)
        {
            _suppressNextClick = true;
        }

        Menu = null;
        OriginalIndex = -1;
        CurrentIndex = -1;
        PendingTargetIndex = -1;
        StartX = 0d;
        StartY = 0d;
        IsActive = false;
        HasMoved = false;
        return new WorkspaceGroupTabDragCompletion(moved, finalIndex);
    }

    public bool ConsumeSuppressedClick()
    {
        bool suppress = _suppressNextClick;
        _suppressNextClick = false;
        return suppress;
    }
}
