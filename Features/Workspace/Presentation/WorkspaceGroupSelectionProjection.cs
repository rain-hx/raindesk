namespace raindesk.Features.Workspace.Presentation;

/// <summary>
/// Projects a selected workspace index onto the synthetic "all groups" tab and real groups.
/// </summary>
public static class WorkspaceGroupSelectionProjection
{
    public static int ToTabIndex(int selectedGroupIndex) =>
        selectedGroupIndex < 0 ? 0 : selectedGroupIndex + 1;

    public static T? ResolveTab<T>(IReadOnlyList<T> tabs, int selectedGroupIndex)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(tabs);
        int tabIndex = ToTabIndex(selectedGroupIndex);
        return tabIndex >= 0 && tabIndex < tabs.Count
            ? tabs[tabIndex]
            : null;
    }

    public static void Apply<T>(
        T allGroupsEntry,
        IReadOnlyList<T> groups,
        int selectedGroupIndex,
        Action<T, bool> setSelected)
    {
        ArgumentNullException.ThrowIfNull(allGroupsEntry);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(setSelected);

        setSelected(allGroupsEntry, selectedGroupIndex < 0);
        for (int index = 0; index < groups.Count; index++)
        {
            setSelected(groups[index], index == selectedGroupIndex);
        }
    }

    public static bool IsSelectedTab<T>(
        T candidate,
        T allGroupsEntry,
        T? activeGroup,
        bool showingAllGroups)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(allGroupsEntry);
        return showingAllGroups
            ? ReferenceEquals(candidate, allGroupsEntry)
            : ReferenceEquals(candidate, activeGroup);
    }
}
