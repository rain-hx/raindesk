using raindesk.Features.Workspace.Contracts;
using raindesk.Constant;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;

namespace raindesk.Features.Workspace.Infrastructure;

public sealed class AppDataWorkspaceQueryHandler : IWorkspaceQueryHandler
{
    private readonly IAppDataProvider _dataProvider;

    public AppDataWorkspaceQueryHandler(IAppDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public IReadOnlyList<WorkspaceGroupSummary> GetGroups()
    {
        return _dataProvider.Read(data => data.MenuList
            .Select((menu, index) => new WorkspaceGroupSummary(
                menu.MenuId,
                menu.MenuName,
                index,
                menu.IconList.Count,
                menu.IsEncrypt))
            .ToList());
    }
}

public sealed class AppDataWorkspaceCommandHandler : IWorkspaceCommandHandler
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;

    public AppDataWorkspaceCommandHandler(IAppDataProvider dataProvider, ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    public WorkspaceCommandResult Create(CreateWorkspaceGroupCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string name = command.Name?.Trim() ?? string.Empty;
        if (name.Length == 0)
        {
            return WorkspaceCommandResult.Failed("分组名称不能为空。");
        }

        string? linkPath = command.LinkPath?.Trim();
        if (command.Kind == WorkspaceGroupKind.LinkedFolder
            && string.IsNullOrWhiteSpace(linkPath))
        {
            return WorkspaceCommandResult.Failed("关联文件夹路径不能为空。");
        }

        string id = Guid.NewGuid().ToString("N");
        _dataProvider.Write(data => data.MenuList.Add(new MenuInfo
        {
            MenuId = id,
            MenuName = name,
            MenuType = command.Kind == WorkspaceGroupKind.LinkedFolder
                ? MenuType.LINK
                : MenuType.NORMAL,
            LinkPath = command.Kind == WorkspaceGroupKind.LinkedFolder ? linkPath : null
        }));
        _saveService.SaveDebounced();
        return new WorkspaceCommandResult(true, true, id);
    }

    public WorkspaceCommandResult Rename(RenameWorkspaceGroupCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string name = command.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command.GroupId) || name.Length == 0)
        {
            return WorkspaceCommandResult.Failed("分组 ID 和名称不能为空。");
        }

        bool found = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            MenuInfo? menu = FindGroup(data, command.GroupId);
            if (menu == null)
            {
                return;
            }

            found = true;
            if (!string.Equals(menu.MenuName, name, StringComparison.Ordinal))
            {
                menu.MenuName = name;
                changed = true;
            }
        });

        if (!found)
        {
            return WorkspaceCommandResult.Failed("未找到分组。");
        }

        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new WorkspaceCommandResult(true, changed, command.GroupId);
    }

    public WorkspaceCommandResult Delete(DeleteWorkspaceGroupCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.GroupId))
        {
            return WorkspaceCommandResult.Failed("分组 ID 不能为空。");
        }

        bool removed = false;
        _dataProvider.Write(data =>
        {
            MenuInfo? menu = FindGroup(data, command.GroupId);
            if (menu == null)
            {
                return;
            }

            int removedIndex = data.MenuList.IndexOf(menu);
            MenuInfo? selected = data.AppConfig.SelectedMenuIndex >= 0
                                 && data.AppConfig.SelectedMenuIndex < data.MenuList.Count
                ? data.MenuList[data.AppConfig.SelectedMenuIndex]
                : null;
            removed = data.MenuList.Remove(menu);
            if (!removed)
            {
                return;
            }

            bool removedWasSelected = selected == null || ReferenceEquals(selected, menu);
            data.AppConfig.SelectedMenuIndex = removedWasSelected
                ? -1
                : data.MenuList.IndexOf(selected!);
            data.AppConfig.SelectedGroupId = removedWasSelected
                ? null
                : selected!.MenuId;
            if (removedIndex < 0)
            {
                data.AppConfig.SelectedMenuIndex = -1;
                data.AppConfig.SelectedGroupId = null;
            }
        });

        if (!removed)
        {
            return WorkspaceCommandResult.Failed("未找到分组。");
        }

        _saveService.SaveDebounced();
        return new WorkspaceCommandResult(true, true, command.GroupId);
    }

    public WorkspaceCommandResult Reorder(ReorderWorkspaceGroupsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        IReadOnlyList<string> ids = command.OrderedGroupIds ?? Array.Empty<string>();
        bool valid = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            if (ids.Count != data.MenuList.Count
                || ids.Distinct(StringComparer.Ordinal).Count() != ids.Count)
            {
                return;
            }

            var byId = data.MenuList.ToDictionary(menu => menu.MenuId, StringComparer.Ordinal);
            if (ids.Any(id => !byId.ContainsKey(id)))
            {
                return;
            }

            valid = true;
            MenuInfo? selected = data.AppConfig.SelectedMenuIndex >= 0
                                 && data.AppConfig.SelectedMenuIndex < data.MenuList.Count
                ? data.MenuList[data.AppConfig.SelectedMenuIndex]
                : null;
            for (int targetIndex = 0; targetIndex < ids.Count; targetIndex++)
            {
                MenuInfo menu = byId[ids[targetIndex]];
                int currentIndex = data.MenuList.IndexOf(menu);
                if (currentIndex != targetIndex)
                {
                    data.MenuList.Move(currentIndex, targetIndex);
                    changed = true;
                }
            }

            data.AppConfig.SelectedMenuIndex = selected == null
                ? -1
                : data.MenuList.IndexOf(selected);
            // G4: SelectedGroupId 按 ID 稳定跟踪，重排后无需更新
            data.AppConfig.SelectedGroupId = selected?.MenuId;
        });

        if (!valid)
        {
            return WorkspaceCommandResult.Failed("分组顺序必须完整且 ID 唯一。");
        }

        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new WorkspaceCommandResult(true, changed);
    }

    public WorkspaceCommandResult Select(SelectWorkspaceGroupCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.GroupIndex < 0 || command.GroupIndex >= _dataProvider.Current.MenuList.Count)
        {
            return WorkspaceCommandResult.Failed("分组索引超出范围。");
        }

        _dataProvider.Write(data =>
        {
            data.AppConfig.SelectedMenuIndex = command.GroupIndex;
            data.AppConfig.SelectedGroupId = data.MenuList[command.GroupIndex].MenuId;
            data.AppConfig.SelectedMenuIcons = data.MenuList[command.GroupIndex].IconList;
        });
        _saveService.SaveDebounced();
        return new WorkspaceCommandResult(true, true);
    }

    public WorkspaceCommandResult SelectAll(SelectAllGroupsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _dataProvider.Write(data =>
        {
            data.AppConfig.SelectedMenuIndex = -1;
            data.AppConfig.SelectedGroupId = null;
            data.AppConfig.SelectedMenuIcons = null;
        });
        _saveService.SaveDebounced();
        return new WorkspaceCommandResult(true, true);
    }

    public WorkspaceCommandResult SetEncryption(SetGroupEncryptionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.GroupId))
        {
            return WorkspaceCommandResult.Failed("分组 ID 不能为空。");
        }

        bool found = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            MenuInfo? menu = FindGroup(data, command.GroupId);
            if (menu == null)
            {
                return;
            }

            found = true;
            if (menu.IsEncrypt != command.IsEncrypted)
            {
                menu.IsEncrypt = command.IsEncrypted;
                changed = true;
            }
        });

        if (!found)
        {
            return WorkspaceCommandResult.Failed("未找到分组。");
        }

        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new WorkspaceCommandResult(true, changed, command.GroupId);
    }

    private static MenuInfo? FindGroup(AppData data, string groupId) =>
        data.MenuList.FirstOrDefault(menu =>
            string.Equals(menu.MenuId, groupId, StringComparison.Ordinal));
}
