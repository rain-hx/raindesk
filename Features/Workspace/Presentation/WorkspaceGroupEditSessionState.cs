namespace raindesk.Features.Workspace.Presentation;

internal readonly record struct WorkspaceGroupEditCompletion<T>(
    T? Target,
    string OriginalName)
    where T : class;

/// <summary>
/// Workspace 分组重命名的纯编辑会话与失焦提交规则。
/// </summary>
internal sealed class WorkspaceGroupEditSessionState<T>
    where T : class
{
    public T? Target { get; private set; }

    public string OriginalName { get; private set; } = string.Empty;

    public bool SuppressLostFocus { get; private set; }

    public bool FocusEstablished { get; private set; }

    public bool HasSession => Target != null;

    public void Begin(T target, string? originalName)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target;
        OriginalName = originalName ?? string.Empty;
        SuppressLostFocus = true;
        FocusEstablished = false;
    }

    public bool IsTarget(T? target) =>
        target != null && ReferenceEquals(Target, target);

    public bool TryEstablishFocus()
    {
        if (!HasSession || FocusEstablished)
        {
            return false;
        }

        FocusEstablished = true;
        return true;
    }

    public void AllowLostFocus() => SuppressLostFocus = false;

    public bool ShouldCommitOutside(bool pointerInsideEditor) =>
        HasSession && !SuppressLostFocus && !pointerInsideEditor;

    public bool ShouldCommitLostFocus(T? target) =>
        HasSession && !SuppressLostFocus && IsTarget(target);

    public WorkspaceGroupEditCompletion<T> Complete()
    {
        var completion = new WorkspaceGroupEditCompletion<T>(Target, OriginalName);
        Target = null;
        OriginalName = string.Empty;
        SuppressLostFocus = false;
        FocusEstablished = false;
        return completion;
    }
}
