using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using rainspace.ViewModel;

namespace rainspace.Features.Workspace.Presentation;

/// <summary>
/// Workspace 分组标签的展示状态。
/// 只负责维护“全部”入口与真实分组的镜像顺序，不执行持久化或业务命令。
/// </summary>
public sealed class WorkspaceGroupTabState
{
    private int _sourceSynchronizationSuppressionDepth;

    public WorkspaceGroupTabState()
    {
        AllGroupsEntry = new MenuInfo
        {
            MenuName = "全部",
            MenuEdit = Visibility.Collapsed,
            IsAllGroupsTab_NoWrite = true
        };
        Items.Add(AllGroupsEntry);
    }

    public MenuInfo AllGroupsEntry { get; }

    public ObservableCollection<MenuInfo> Items { get; } = new();

    public bool IsSourceSynchronizationSuppressed =>
        _sourceSynchronizationSuppressionDepth > 0;

    public IDisposable SuppressSourceSynchronization()
    {
        _sourceSynchronizationSuppressionDepth++;
        return new SourceSynchronizationScope(this);
    }

    public void Reset(IEnumerable<MenuInfo> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        Items.Clear();
        Items.Add(AllGroupsEntry);
        foreach (MenuInfo group in groups.Where(group => group != null))
        {
            Items.Add(group);
        }
    }

    public void ApplyCollectionChange(
        NotifyCollectionChangedEventArgs change,
        IEnumerable<MenuInfo> currentGroups)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentNullException.ThrowIfNull(currentGroups);

        if (IsSourceSynchronizationSuppressed
            || change.Action == NotifyCollectionChangedAction.Move)
        {
            // UI 拖动或先行删除时 Items 已完成变更；随后命令同步数据集合，
            // 不能再把同一个变更反向投影一次。
            return;
        }

        switch (change.Action)
        {
            case NotifyCollectionChangedAction.Add:
                InsertNewGroups(change);
                break;
            case NotifyCollectionChangedAction.Remove:
                RemoveOldGroups(change);
                break;
            case NotifyCollectionChangedAction.Replace:
                RemoveOldGroups(change);
                InsertNewGroups(change);
                break;
            case NotifyCollectionChangedAction.Reset:
                Reset(currentGroups);
                break;
        }
    }

    public IReadOnlyList<string> GetOrderedGroupIds() => Items
        .Where(item => !item.IsAllGroupsTab_NoWrite)
        .Select(item => item.MenuId)
        .ToArray();

    public bool Remove(MenuInfo group) => Items.Remove(group);

    private void InsertNewGroups(NotifyCollectionChangedEventArgs change)
    {
        if (change.NewItems == null)
        {
            return;
        }

        int insertionIndex = change.NewStartingIndex >= 0
            ? Math.Min(Items.Count, change.NewStartingIndex + 1)
            : Items.Count;
        foreach (MenuInfo group in change.NewItems.OfType<MenuInfo>())
        {
            if (Items.Contains(group))
            {
                continue;
            }

            Items.Insert(insertionIndex, group);
            insertionIndex++;
        }
    }

    private void RemoveOldGroups(NotifyCollectionChangedEventArgs change)
    {
        if (change.OldItems == null)
        {
            return;
        }

        foreach (MenuInfo group in change.OldItems.OfType<MenuInfo>())
        {
            Items.Remove(group);
        }
    }

    private void RestoreSourceSynchronization()
    {
        if (_sourceSynchronizationSuppressionDepth > 0)
        {
            _sourceSynchronizationSuppressionDepth--;
        }
    }

    private sealed class SourceSynchronizationScope : IDisposable
    {
        private WorkspaceGroupTabState? _owner;

        public SourceSynchronizationScope(WorkspaceGroupTabState owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            WorkspaceGroupTabState? owner = Interlocked.Exchange(ref _owner, null);
            owner?.RestoreSourceSynchronization();
        }
    }
}
