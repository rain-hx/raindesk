namespace raindesk.Features.Workspace.Presentation;

public readonly record struct WorkspaceNavigationSnapshot(string? SelectedGroupId)
{
    public static WorkspaceNavigationSnapshot AllGroups { get; } = new(null);

    public bool IsAllGroups => SelectedGroupId == null;
}

/// <summary>
/// Workspace 分组导航的纯状态。
/// 以稳定 GroupId 保存选择，集合索引只作为旧配置的持久化投影。
/// </summary>
public sealed class WorkspaceNavigationState
{
    public bool IsAllGroups => SelectedGroupId == null;

    public string? SelectedGroupId { get; private set; }

    public void SelectAll() => SelectedGroupId = null;

    public WorkspaceNavigationSnapshot Capture() => new(SelectedGroupId);

    public bool TrySelect(string? groupId, IReadOnlyList<string> availableGroupIds)
    {
        ArgumentNullException.ThrowIfNull(availableGroupIds);
        if (string.IsNullOrWhiteSpace(groupId)
            || !availableGroupIds.Contains(groupId, StringComparer.Ordinal))
        {
            return false;
        }

        SelectedGroupId = groupId;
        return true;
    }

    public bool RestoreFromPersistedIndex(
        int selectedIndex,
        IReadOnlyList<string> availableGroupIds)
    {
        ArgumentNullException.ThrowIfNull(availableGroupIds);
        if (selectedIndex < 0 || selectedIndex >= availableGroupIds.Count)
        {
            SelectAll();
            return false;
        }

        return TrySelect(availableGroupIds[selectedIndex], availableGroupIds);
    }

    public bool Restore(
        WorkspaceNavigationSnapshot snapshot,
        IReadOnlyList<string> availableGroupIds)
    {
        ArgumentNullException.ThrowIfNull(availableGroupIds);
        if (snapshot.IsAllGroups)
        {
            SelectAll();
            return true;
        }

        if (TrySelect(snapshot.SelectedGroupId, availableGroupIds))
        {
            return true;
        }

        SelectAll();
        return false;
    }

    /// <summary>
    /// 在分组集合变化后保留仍存在的稳定选择；被选分组消失时回退到“全部”。
    /// 返回选择是否发生变化。
    /// </summary>
    public bool Reconcile(IReadOnlyList<string> availableGroupIds)
    {
        ArgumentNullException.ThrowIfNull(availableGroupIds);
        if (IsAllGroups
            || availableGroupIds.Contains(SelectedGroupId!, StringComparer.Ordinal))
        {
            return false;
        }

        SelectAll();
        return true;
    }

    public int GetSelectedIndex(IReadOnlyList<string> availableGroupIds)
    {
        ArgumentNullException.ThrowIfNull(availableGroupIds);
        if (SelectedGroupId == null)
        {
            return -1;
        }

        for (int i = 0; i < availableGroupIds.Count; i++)
        {
            if (string.Equals(availableGroupIds[i], SelectedGroupId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
