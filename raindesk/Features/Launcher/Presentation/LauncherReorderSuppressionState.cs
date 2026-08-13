namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// Tracks independent reasons that temporarily suppress one reorder surface.
/// </summary>
internal sealed class LauncherReorderSuppressionState<TReason>
    where TReason : notnull
{
    private readonly HashSet<TReason> _reasons = new();

    public bool IsSuppressed => _reasons.Count > 0;

    public int ReasonCount => _reasons.Count;

    public bool Contains(TReason reason) => _reasons.Contains(reason);

    public bool Suppress(TReason reason) => _reasons.Add(reason);

    public bool Restore(TReason reason) => _reasons.Remove(reason);

    public void Clear() => _reasons.Clear();
}
