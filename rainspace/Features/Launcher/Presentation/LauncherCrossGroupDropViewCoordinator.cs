namespace rainspace.Features.Launcher.Presentation;

internal readonly record struct LauncherCrossGroupDropViewUpdate(
    bool RestoredAllGroups,
    bool AppliedCapturedOrder,
    int SelectedGroupIndex);

/// <summary>
/// Applies a committed cross-group move to the current Launcher projection.
/// All concrete navigation, view and persistence operations are supplied by the composition root.
/// </summary>
internal sealed class LauncherCrossGroupDropViewCoordinator<TItem>
{
    private readonly Func<IReadOnlyList<TItem>?> _getAllGroupsOrder;
    private readonly Action _selectAllGroups;
    private readonly Action<IReadOnlyList<TItem>> _applyAllGroupsOrder;
    private readonly Action _rebuildTiles;
    private readonly Action _refreshLayout;
    private readonly Action<int> _refreshGroupSelection;
    private readonly Action _persist;

    public LauncherCrossGroupDropViewCoordinator(
        Func<IReadOnlyList<TItem>?> getAllGroupsOrder,
        Action selectAllGroups,
        Action<IReadOnlyList<TItem>> applyAllGroupsOrder,
        Action rebuildTiles,
        Action refreshLayout,
        Action<int> refreshGroupSelection,
        Action persist)
    {
        _getAllGroupsOrder = getAllGroupsOrder
            ?? throw new ArgumentNullException(nameof(getAllGroupsOrder));
        _selectAllGroups = selectAllGroups
            ?? throw new ArgumentNullException(nameof(selectAllGroups));
        _applyAllGroupsOrder = applyAllGroupsOrder
            ?? throw new ArgumentNullException(nameof(applyAllGroupsOrder));
        _rebuildTiles = rebuildTiles ?? throw new ArgumentNullException(nameof(rebuildTiles));
        _refreshLayout = refreshLayout ?? throw new ArgumentNullException(nameof(refreshLayout));
        _refreshGroupSelection = refreshGroupSelection
            ?? throw new ArgumentNullException(nameof(refreshGroupSelection));
        _persist = persist ?? throw new ArgumentNullException(nameof(persist));
    }

    public LauncherCrossGroupDropViewUpdate Apply(
        bool wasAllGroups,
        int activeGroupIndex,
        Action? beforePersist = null)
    {
        bool appliedCapturedOrder = false;
        if (wasAllGroups)
        {
            _selectAllGroups();
            IReadOnlyList<TItem>? currentOrder = _getAllGroupsOrder();
            if (currentOrder is { Count: > 0 })
            {
                _applyAllGroupsOrder(currentOrder);
                appliedCapturedOrder = true;
            }
            else
            {
                _rebuildTiles();
            }
        }
        else
        {
            _rebuildTiles();
        }

        int selectedGroupIndex = wasAllGroups ? -1 : activeGroupIndex;
        _refreshLayout();
        _refreshGroupSelection(selectedGroupIndex);
        beforePersist?.Invoke();
        _persist();

        return new LauncherCrossGroupDropViewUpdate(
            wasAllGroups,
            appliedCapturedOrder,
            selectedGroupIndex);
    }
}
