using raindesk.Features.Workspace.Contracts;
using raindesk.ViewModel;

namespace raindesk.Features.Workspace.Presentation;

/// <summary>
/// 将稳定 GroupId 导航状态投影到迁移期 AppData 配置。
/// RightCard 只消费选择结果，不再自行维护索引兼容逻辑。
/// </summary>
public sealed class WorkspaceNavigationPresenter
{
    private readonly WorkspaceNavigationState _state = new();
    private readonly IWorkspaceCommandHandler? _commandHandler;

    public WorkspaceNavigationPresenter(IWorkspaceCommandHandler? commandHandler = null)
    {
        _commandHandler = commandHandler;
    }

    public bool IsAllGroups => _state.IsAllGroups;

    public string? SelectedGroupId => _state.SelectedGroupId;

    public WorkspaceNavigationSnapshot Capture() => _state.Capture();

    public MenuInfo? GetActiveGroup(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        string? selectedGroupId = _state.SelectedGroupId;
        return selectedGroupId == null
            ? null
            : data.MenuList.FirstOrDefault(menu =>
                string.Equals(menu.MenuId, selectedGroupId, StringComparison.Ordinal));
    }

    public int GetActiveGroupIndex(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _state.GetSelectedIndex(GetGroupIds(data));
    }

    public void SelectAll(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        _state.SelectAll();
        if (_commandHandler != null)
        {
            _commandHandler.SelectAll(new SelectAllGroupsCommand());
        }
        else
        {
            data.AppConfig.SelectedMenuIndex = -1;
            data.AppConfig.SelectedGroupId = null;
            data.AppConfig.SelectedMenuIcons = null;
        }
    }

    public bool TrySelect(AppData data, int index)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (index < 0 || index >= data.MenuList.Count)
        {
            return false;
        }

        if (!_state.TrySelect(data.MenuList[index].MenuId, GetGroupIds(data)))
        {
            return false;
        }

        if (_commandHandler != null)
        {
            _commandHandler.Select(new SelectWorkspaceGroupCommand(index));
        }
        else
        {
            data.AppConfig.SelectedMenuIndex = index;
            data.AppConfig.SelectedGroupId = data.MenuList[index].MenuId;
            data.AppConfig.SelectedMenuIcons = data.MenuList[index].IconList;
        }
        return true;
    }

    public bool Restore(AppData data, WorkspaceNavigationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(data);
        bool restored = _state.Restore(snapshot, GetGroupIds(data));
        int activeIndex = GetActiveGroupIndex(data);
        if (_commandHandler != null)
        {
            if (_state.IsAllGroups)
            {
                _commandHandler.SelectAll(new SelectAllGroupsCommand());
            }
            else if (activeIndex >= 0)
            {
                _commandHandler.Select(new SelectWorkspaceGroupCommand(activeIndex));
            }
        }
        else
        {
            data.AppConfig.SelectedMenuIndex = activeIndex;
            if (_state.IsAllGroups)
            {
                data.AppConfig.SelectedGroupId = null;
                data.AppConfig.SelectedMenuIcons = null;
            }
            else if (activeIndex >= 0 && activeIndex < data.MenuList.Count)
            {
                data.AppConfig.SelectedGroupId = data.MenuList[activeIndex].MenuId;
            }
        }

        return restored;
    }

    /// <summary>
    /// 在分组增删或重排后维护稳定选择，并刷新旧索引投影。
    /// 返回当前选择是否因为分组消失而改变。
    /// </summary>
    public bool Reconcile(AppData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        bool changed = _state.Reconcile(GetGroupIds(data));
        int activeIndex = GetActiveGroupIndex(data);
        if (_commandHandler != null)
        {
            if (changed)
            {
                _commandHandler.SelectAll(new SelectAllGroupsCommand());
            }
            else if (activeIndex >= 0)
            {
                _commandHandler.Select(new SelectWorkspaceGroupCommand(activeIndex));
            }
        }
        else
        {
            data.AppConfig.SelectedMenuIndex = activeIndex;
            if (changed)
            {
                data.AppConfig.SelectedGroupId = null;
                data.AppConfig.SelectedMenuIcons = null;
            }
            else if (activeIndex >= 0 && activeIndex < data.MenuList.Count)
            {
                data.AppConfig.SelectedGroupId = data.MenuList[activeIndex].MenuId;
            }
        }

        return changed;
    }

    private static string[] GetGroupIds(AppData data) => data.MenuList
        .Select(menu => menu.MenuId)
        .ToArray();
}
