namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherCrossGroupPreviewUpdate<TItem>(
    bool SessionStarted,
    bool TargetChanged,
    int Token,
    IReadOnlyList<TItem> Tiles,
    int TargetInsertIndex);

internal enum LauncherCrossGroupPreviewCancelMode
{
    None,
    PreserveBatchPlaceholders,
    RestoreNavigation
}

internal enum LauncherCrossGroupNavigationRestoreMode
{
    RestoreAllGroups,
    RestoreActiveGroup,
    RebuildFallback
}

internal readonly record struct LauncherCrossGroupPreviewCancelPlan(
    LauncherCrossGroupPreviewCancelMode Mode,
    bool RestoreBatchItems,
    bool CapturePreviousPositions,
    bool RecommitSingleIconTarget);

internal static class LauncherCrossGroupPreviewCompletionPolicy
{
    public static LauncherCrossGroupPreviewCancelPlan PlanCancel(
        bool isActive,
        bool draggedItemsSuppressed,
        bool customDragActive,
        bool reducedMotion,
        bool hasDraggedItem)
    {
        if (!isActive)
        {
            return new LauncherCrossGroupPreviewCancelPlan(
                LauncherCrossGroupPreviewCancelMode.None,
                false,
                false,
                false);
        }

        bool preserveBatchPlaceholders = draggedItemsSuppressed && customDragActive;
        return new LauncherCrossGroupPreviewCancelPlan(
            preserveBatchPlaceholders
                ? LauncherCrossGroupPreviewCancelMode.PreserveBatchPlaceholders
                : LauncherCrossGroupPreviewCancelMode.RestoreNavigation,
            RestoreBatchItems: draggedItemsSuppressed && !preserveBatchPlaceholders,
            CapturePreviousPositions: !preserveBatchPlaceholders && !reducedMotion,
            RecommitSingleIconTarget: !preserveBatchPlaceholders
                && customDragActive
                && hasDraggedItem
                && !draggedItemsSuppressed);
    }

    public static LauncherCrossGroupNavigationRestoreMode ResolveNavigation(
        bool selectionRestored,
        bool showingAllGroups,
        bool hasActiveGroup)
    {
        if (!selectionRestored || showingAllGroups)
        {
            return LauncherCrossGroupNavigationRestoreMode.RestoreAllGroups;
        }

        return hasActiveGroup
            ? LauncherCrossGroupNavigationRestoreMode.RestoreActiveGroup
            : LauncherCrossGroupNavigationRestoreMode.RebuildFallback;
    }
}

/// <summary>
/// Combines cross-group session versioning with target preview planning.
/// UI side effects are reported as decisions and remain outside this pure coordinator.
/// </summary>
internal sealed class LauncherCrossGroupPreviewCoordinator<TTarget, TSnapshot, TItem>
    where TTarget : class
    where TItem : class
{
    private readonly LauncherCrossGroupPreviewState<TTarget, TSnapshot> _state;

    public LauncherCrossGroupPreviewCoordinator(TSnapshot initialSelection)
    {
        _state = new LauncherCrossGroupPreviewState<TTarget, TSnapshot>(initialSelection);
    }

    public bool IsActive => _state.IsActive;

    public TTarget? Target => _state.Target;

    public TSnapshot SavedSelection => _state.SavedSelection;

    public int Token => _state.Token;

    public bool IsCurrent(int token) => _state.IsCurrent(token);

    public LauncherCrossGroupPreviewCancelPlan Cancel(
        bool draggedItemsSuppressed,
        bool customDragActive,
        bool reducedMotion,
        bool hasDraggedItem)
    {
        LauncherCrossGroupPreviewCancelPlan plan =
            LauncherCrossGroupPreviewCompletionPolicy.PlanCancel(
                _state.IsActive,
                draggedItemsSuppressed,
                customDragActive,
                reducedMotion,
                hasDraggedItem);
        if (plan.Mode != LauncherCrossGroupPreviewCancelMode.None)
        {
            _state.End();
        }

        return plan;
    }

    public bool Commit()
    {
        bool wasActive = _state.IsActive;
        _state.End();
        return wasActive;
    }

    public LauncherCrossGroupPreviewUpdate<TItem> Retarget(
        TTarget target,
        TSnapshot savedSelection,
        IEnumerable<TItem>? targetItems,
        IReadOnlyList<TItem> draggedItems,
        Func<TItem, bool> exclude)
    {
        ArgumentNullException.ThrowIfNull(target);
        bool sessionStarted = _state.Begin(savedSelection);
        if (!_state.TryRetarget(target, out int token))
        {
            return new LauncherCrossGroupPreviewUpdate<TItem>(
                sessionStarted,
                false,
                token,
                Array.Empty<TItem>(),
                0);
        }

        LauncherCrossGroupPreviewTilePlan<TItem> plan =
            LauncherCrossGroupPreviewTilePlanner.Plan(
                targetItems,
                draggedItems,
                exclude);
        return new LauncherCrossGroupPreviewUpdate<TItem>(
            sessionStarted,
            true,
            token,
            plan.Items,
            plan.TargetInsertIndex);
    }

    public void End() => _state.End();
}
