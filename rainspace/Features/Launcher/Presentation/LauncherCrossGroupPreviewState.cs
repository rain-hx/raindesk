namespace rainspace.Features.Launcher.Presentation;

/// <summary>
/// 跨分组拖拽预览的纯会话状态。负责保存进入预览前的选择、当前目标，
/// 并通过单调递增的 token 使已排队的异步预览更新失效。
/// </summary>
internal sealed class LauncherCrossGroupPreviewState<TTarget, TSnapshot>
    where TTarget : class
{
    public LauncherCrossGroupPreviewState(TSnapshot initialSelection)
    {
        SavedSelection = initialSelection;
    }

    public bool IsActive { get; private set; }

    public TTarget? Target { get; private set; }

    public TSnapshot SavedSelection { get; private set; }

    public int Token { get; private set; }

    /// <summary>
    /// 开始一次预览会话。活跃会话不会覆盖最初捕获的选择。
    /// </summary>
    public bool Begin(TSnapshot savedSelection)
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        Target = null;
        SavedSelection = savedSelection;
        return true;
    }

    /// <summary>
    /// 切换预览目标。目标未变化时不创建新版本。
    /// </summary>
    public bool TryRetarget(TTarget target, out int token)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!IsActive)
        {
            throw new InvalidOperationException("Cross-group preview session has not started.");
        }

        if (ReferenceEquals(Target, target))
        {
            token = Token;
            return false;
        }

        Target = target;
        token = ++Token;
        return true;
    }

    public bool IsCurrent(int token) =>
        IsActive && token == Token;

    /// <summary>
    /// 结束会话并使此前排队的更新全部失效；保留 SavedSelection 供收尾流程判断来源视图。
    /// </summary>
    public void End()
    {
        Token++;
        IsActive = false;
        Target = null;
    }
}
