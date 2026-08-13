namespace raindesk.Features.Launcher.Presentation;

internal readonly record struct LauncherSelectionPoint(double X, double Y);

internal readonly record struct LauncherSelectionRange(int StartIndex, int EndIndex);

internal readonly record struct LauncherMarqueeSnapshot<TItem>(
    LauncherSelectionPoint Start,
    bool Additive,
    IReadOnlyList<TItem> BaselineSelection);

/// <summary>
/// Owns selection anchor, marquee pointer identity/baseline and temporary drag-visual snapshots.
/// It is independent of WinUI and the concrete shortcut model.
/// </summary>
internal sealed class LauncherIconSelectionState<TItem>
    where TItem : notnull
{
    private uint _marqueePointerId;
    private LauncherSelectionPoint _marqueeStart;
    private bool _marqueeAdditive;
    private List<TItem> _marqueeBaseline = new();
    private readonly Dictionary<TItem, bool> _dragVisualSnapshots = new();

    public int AnchorIndex { get; private set; } = -1;

    public bool IsMarqueeActive { get; private set; }

    public void SetAnchor(int index)
    {
        AnchorIndex = index >= 0 ? index : -1;
    }

    public void ResetAnchor()
    {
        AnchorIndex = -1;
    }

    public LauncherSelectionRange? ResolveRangeTo(int endIndex, int itemCount)
    {
        if (endIndex < 0 || endIndex >= itemCount)
        {
            return null;
        }

        int anchorIndex = AnchorIndex >= 0 && AnchorIndex < itemCount
            ? AnchorIndex
            : endIndex;
        AnchorIndex = endIndex;
        return new LauncherSelectionRange(
            Math.Min(anchorIndex, endIndex),
            Math.Max(anchorIndex, endIndex));
    }

    public void BeginMarquee(
        uint pointerId,
        LauncherSelectionPoint start,
        bool additive,
        IEnumerable<TItem> baselineSelection)
    {
        ArgumentNullException.ThrowIfNull(baselineSelection);
        _marqueePointerId = pointerId;
        _marqueeStart = start;
        _marqueeAdditive = additive;
        _marqueeBaseline = additive
            ? baselineSelection.Distinct().ToList()
            : new List<TItem>();
        IsMarqueeActive = true;
    }

    public bool TryGetMarquee(uint pointerId, out LauncherMarqueeSnapshot<TItem> snapshot)
    {
        if (!IsMarqueeActive || pointerId != _marqueePointerId)
        {
            snapshot = default;
            return false;
        }

        snapshot = new LauncherMarqueeSnapshot<TItem>(
            _marqueeStart,
            _marqueeAdditive,
            _marqueeBaseline);
        return true;
    }

    public bool CompleteMarquee(uint pointerId)
    {
        if (!IsMarqueeActive || pointerId != _marqueePointerId)
        {
            return false;
        }

        IsMarqueeActive = false;
        _marqueePointerId = 0;
        _marqueeBaseline = new List<TItem>();
        return true;
    }

    public void CaptureDragVisuals(IEnumerable<KeyValuePair<TItem, bool>> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        _dragVisualSnapshots.Clear();
        foreach (KeyValuePair<TItem, bool> snapshot in snapshots)
        {
            _dragVisualSnapshots[snapshot.Key] = snapshot.Value;
        }
    }

    public IReadOnlyList<KeyValuePair<TItem, bool>> TakeDragVisuals()
    {
        KeyValuePair<TItem, bool>[] snapshots = _dragVisualSnapshots.ToArray();
        _dragVisualSnapshots.Clear();
        return snapshots;
    }
}
