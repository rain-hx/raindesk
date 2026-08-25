using raindesk.Features.Launcher.Contracts;
using raindesk.Constant;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;
using System.Collections.ObjectModel;

namespace raindesk.Features.Launcher.Infrastructure;

public sealed class AppDataLauncherCommandHandler : ILauncherCommandHandler
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;

    public AppDataLauncherCommandHandler(IAppDataProvider dataProvider, ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    public LauncherCommandResult Execute(LauncherMutationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Kind is LauncherMutationKind.RemoveShortcut
            or LauncherMutationKind.ResizeFolder
            or LauncherMutationKind.MoveOutOfFolder)
        {
            return ExecuteShortcutMutation(command);
        }

        bool requestValid = false;
        bool committed = false;
        _dataProvider.Write(data =>
        {
            LauncherDropRequest? request = BuildRequest(data, command);
            if (request == null)
            {
                return;
            }

            requestValid = true;
            committed = LauncherDropCommitService.Commit(data, request).Committed;
        });

        if (!requestValid)
        {
            return LauncherCommandResult.Failed("Launcher 命令引用了不存在的分组或快捷方式。");
        }

        if (committed)
        {
            _saveService.SaveDebounced();
        }
        return new LauncherCommandResult(true, committed);
    }

    private LauncherCommandResult ExecuteShortcutMutation(LauncherMutationCommand command)
    {
        bool requestValid = false;
        bool committed = false;
        _dataProvider.Write(data =>
        {
            IconInfo? shortcut = command.Kind == LauncherMutationKind.ResizeFolder
                ? FindLayoutIcon(data, command.SubjectShortcutId ?? string.Empty)
                : FindIcon(data, command.SubjectShortcutId);
            if (shortcut == null)
            {
                return;
            }

            switch (command.Kind)
            {
                case LauncherMutationKind.RemoveShortcut:
                    requestValid = true;
                    committed = ShortcutFolderTreeService.RemoveFromTree(data.MenuList, shortcut);
                    break;

                case LauncherMutationKind.ResizeFolder when shortcut.IsFolder && command.FolderSize.HasValue:
                    requestValid = true;
                    ShortcutFolderSize targetSize = ToModelFolderSize(command.FolderSize.Value);
                    if (shortcut.FolderSize != targetSize)
                    {
                        committed = ShortcutFolderService.ChangeFolderSize(shortcut, targetSize);
                    }
                    break;

                case LauncherMutationKind.MoveOutOfFolder:
                    IconInfo? folder = FindIcon(data, command.TargetShortcutId);
                    if (folder?.IsFolder != true
                        || !folder.FolderItems.Contains(shortcut)
                        || !ShortcutFolderTreeService.TryFindContainer(data.MenuList, folder, out var container))
                    {
                        return;
                    }

                    requestValid = true;
                    int folderIndex = container.Collection.IndexOf(folder);
                    int insertIndex = command.InsertIndex >= 0
                        ? command.InsertIndex
                        : folderIndex >= 0 ? folderIndex + 1 : container.Collection.Count;
                    committed = ShortcutFolderService.MoveIconOutOfFolder(
                        container.Collection,
                        folder,
                        shortcut,
                        insertIndex,
                        data.MenuList);
                    if (committed)
                    {
                        shortcut.LauncherGridColumn = -1;
                        shortcut.LauncherGridRow = -1;
                    }
                    break;
            }
        });

        if (!requestValid)
        {
            return LauncherCommandResult.Failed("Launcher 上下文命令引用了无效的快捷方式或参数。");
        }

        if (committed)
        {
            _saveService.SaveDebounced();
        }

        return new LauncherCommandResult(true, committed);
    }

    private static ShortcutFolderSize ToModelFolderSize(LauncherFolderSize size) => size switch
    {
        LauncherFolderSize.Small => ShortcutFolderSize.Small,
        LauncherFolderSize.Medium => ShortcutFolderSize.Medium,
        LauncherFolderSize.Large => ShortcutFolderSize.Large,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };

    public LauncherCommandResult Add(AddLauncherShortcutCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Shortcut);
        return AddMany(new AddLauncherShortcutsCommand(
            command.TargetGroupId,
            new[] { command.Shortcut },
            ApplyConfiguredSort: false),
            command.InsertIndex);
    }

    public LauncherCommandResult AddMany(AddLauncherShortcutsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return AddMany(command, insertIndex: -1);
    }

    private LauncherCommandResult AddMany(AddLauncherShortcutsCommand command, int insertIndex)
    {
        if (string.IsNullOrWhiteSpace(command.TargetGroupId)
            || command.Shortcuts == null
            || command.Shortcuts.Count == 0
            || command.Shortcuts.Any(shortcut => shortcut == null || string.IsNullOrWhiteSpace(shortcut.Name)))
        {
            return LauncherCommandResult.Failed("目标分组和快捷方式集合不能为空。");
        }

        bool targetFound = false;
        _dataProvider.Write(data =>
        {
            MenuInfo? target = data.MenuList.FirstOrDefault(menu =>
                string.Equals(menu.MenuId, command.TargetGroupId, StringComparison.Ordinal));
            if (target == null)
            {
                return;
            }

            targetFound = true;
            int nextIndex = insertIndex < 0
                ? target.IconList.Count
                : Math.Min(insertIndex, target.IconList.Count);
            foreach (LauncherShortcutDraft draft in command.Shortcuts)
            {
                target.IconList.Insert(nextIndex++, CreateShortcut(draft));
            }

            if (command.ApplyConfiguredSort)
            {
                SortAllShortcutLists(data);
            }
        });

        if (!targetFound)
        {
            return LauncherCommandResult.Failed("目标分组不存在。");
        }

        _saveService.SaveDebounced();
        return new LauncherCommandResult(true, true);
    }

    private static IconInfo CreateShortcut(LauncherShortcutDraft draft)
    {
        byte[]? image = draft.ImageBytes?.ToArray();
        return new IconInfo
        {
            IconId = Guid.NewGuid().ToString("N"),
            Name = draft.Name.Trim(),
            Path = draft.Path ?? string.Empty,
            StartArg = draft.StartArgument,
            LnkPath = draft.LinkPath,
            IconType = draft.Kind == LauncherShortcutKind.Url
                ? IconType.URL
                : IconType.OTHER,
            ImageByteArr = image,
            DefaultImage = image?.ToArray()
        };
    }

    private static void SortAllShortcutLists(AppData data)
    {
        SortType sortType = data.AppConfig.IconSortType;
        foreach (MenuInfo menu in data.MenuList)
        {
            if (menu.IconList == null || menu.IconList.Count <= 1)
            {
                continue;
            }

            IconInfo? placeholder = menu.IconList.FirstOrDefault(icon => icon.IsAddShortcut_NoWrite);
            List<IconInfo> icons = menu.IconList
                .Where(icon => !icon.IsAddShortcut_NoWrite)
                .ToList();
            IEnumerable<IconInfo> ordered = sortType switch
            {
                SortType.COUNT_UP => icons.OrderBy(icon => icon.Count),
                SortType.COUNT_LOW => icons.OrderByDescending(icon => icon.Count),
                SortType.NAME_UP => icons.OrderBy(icon => icon.Name, StringComparer.OrdinalIgnoreCase),
                SortType.NAME_LOW => icons.OrderByDescending(icon => icon.Name, StringComparer.OrdinalIgnoreCase),
                _ => icons
            };

            menu.IconList.Clear();
            foreach (IconInfo icon in ordered)
            {
                menu.IconList.Add(icon);
            }
            if (placeholder != null)
            {
                menu.IconList.Add(placeholder);
            }
        }
    }

    public LauncherCommandResult Rename(RenameLauncherShortcutCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string name = command.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command.ShortcutId) || string.IsNullOrWhiteSpace(name))
        {
            return LauncherCommandResult.Failed("快捷方式 ID 和名称不能为空。");
        }

        return UpdateShortcut(command.ShortcutId, shortcut =>
        {
            if (string.Equals(shortcut.Name, name, StringComparison.Ordinal))
            {
                return false;
            }

            shortcut.Name = name;
            return true;
        });
    }

    public LauncherCommandResult Update(UpdateLauncherShortcutCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Properties);
        string name = command.Properties.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command.ShortcutId) || string.IsNullOrWhiteSpace(name))
        {
            return LauncherCommandResult.Failed("快捷方式 ID 和名称不能为空。");
        }

        return UpdateShortcut(command.ShortcutId, shortcut =>
        {
            bool changed = false;
            changed |= AssignIfDifferent(shortcut.Name, name, value => shortcut.Name = value);
            changed |= AssignIfDifferent(shortcut.Path, command.Properties.Path ?? string.Empty, value => shortcut.Path = value);
            changed |= AssignIfDifferent(shortcut.StartArg, command.Properties.StartArgument, value => shortcut.StartArg = value);
            if (shortcut.AdminStartUp != command.Properties.AdminStartUp)
            {
                shortcut.AdminStartUp = command.Properties.AdminStartUp;
                changed = true;
            }

            if (command.Properties.ReplaceImage)
            {
                byte[]? replacement = command.Properties.ImageBytes?.ToArray();
                if (!ByteArraysEqual(shortcut.ImageByteArr, replacement))
                {
                    shortcut.ImageByteArr = replacement;
                    changed = true;
                }
            }

            return changed;
        });
    }

    public LauncherCommandResult UpdateGridLayout(UpdateLauncherGridLayoutCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Placements == null
            || command.Placements.Count == 0
            || command.Placements.Any(placement =>
                string.IsNullOrWhiteSpace(placement.ShortcutId)
                || !IsValidGridPlacement(placement))
            || command.Placements
                .Select(placement => placement.ShortcutId)
                .Distinct(StringComparer.Ordinal)
                .Count() != command.Placements.Count)
        {
            return LauncherCommandResult.Failed("Launcher 网格布局命令无效。");
        }

        bool requestValid = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            var resolved = new List<(IconInfo Icon, LauncherGridPlacement Placement)>(
                command.Placements.Count);
            foreach (LauncherGridPlacement placement in command.Placements)
            {
                IconInfo? icon = FindLayoutIcon(data, placement.ShortcutId);
                if (icon == null)
                {
                    return;
                }
                resolved.Add((icon, placement));
            }

            requestValid = true;
            foreach ((IconInfo icon, LauncherGridPlacement placement) in resolved)
            {
                if (icon.LauncherGridColumn == placement.Column
                    && icon.LauncherGridRow == placement.Row)
                {
                    continue;
                }

                icon.LauncherGridColumn = placement.Column;
                icon.LauncherGridRow = placement.Row;
                changed = true;
            }
        });

        if (!requestValid)
        {
            return LauncherCommandResult.Failed("Launcher 网格布局引用了不存在的快捷方式。");
        }
        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new LauncherCommandResult(true, changed);
    }

    public LauncherCommandResult UpdateOrder(UpdateLauncherShortcutOrderCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.OrderedShortcutIds == null
            || command.OrderedShortcutIds.Any(string.IsNullOrWhiteSpace)
            || command.OrderedShortcutIds.Distinct(StringComparer.Ordinal).Count()
                != command.OrderedShortcutIds.Count
            || command.Scope == LauncherShortcutOrderScope.Group
                && string.IsNullOrWhiteSpace(command.TargetGroupId))
        {
            return LauncherCommandResult.Failed("Launcher 顺序命令无效。");
        }

        bool requestValid = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            switch (command.Scope)
            {
                case LauncherShortcutOrderScope.Group:
                    MenuInfo? group = data.MenuList.FirstOrDefault(menu =>
                        string.Equals(menu.MenuId, command.TargetGroupId, StringComparison.Ordinal));
                    if (group == null
                        || !TryResolveRootOrder(group, command.OrderedShortcutIds, out List<IconInfo> ordered))
                    {
                        return;
                    }

                    requestValid = true;
                    changed = ApplyVisibleOrder(group.IconList, ordered);
                    break;

                case LauncherShortcutOrderScope.AllGroups:
                    if (!TryResolveAllGroupsOrder(data, command.OrderedShortcutIds, out List<IconInfo> flat))
                    {
                        return;
                    }

                    requestValid = true;
                    // All Groups is an independent projection. Reordering that projection
                    // must never mutate the source order owned by each real group.
                    changed |= SetAllGroupsFlatOrderKeys(data, flat);
                    break;
            }
        });

        if (!requestValid)
        {
            return LauncherCommandResult.Failed("Launcher 顺序命令引用了不存在或不可见的快捷方式。");
        }
        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new LauncherCommandResult(true, changed);
    }

    public LauncherCommandResult MutateAllGroupsFolder(
        LauncherAllGroupsFolderMutationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(typeof(LauncherAllGroupsFolderMutationKind), command.Kind)
            || string.IsNullOrWhiteSpace(command.SubjectShortcutId)
            || string.IsNullOrWhiteSpace(command.TargetShortcutId)
            || command.FlatOrderShortcutIds == null
            || command.FlatOrderShortcutIds.Count == 0
            || command.FlatOrderShortcutIds.Any(string.IsNullOrWhiteSpace)
            || command.FlatOrderShortcutIds.Distinct(StringComparer.Ordinal).Count()
                != command.FlatOrderShortcutIds.Count)
        {
            return LauncherCommandResult.Failed("All Groups 文件夹命令无效。");
        }

        bool requestValid = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            if (!TryResolveExactAllGroupsOrder(
                    data,
                    command.FlatOrderShortcutIds,
                    out List<IconInfo> flat))
            {
                return;
            }

            IconInfo? subject = FindLayoutIcon(data, command.SubjectShortcutId);
            IconInfo? target = FindLayoutIcon(data, command.TargetShortcutId);
            if (subject == null || target == null)
            {
                return;
            }

            bool mutated;
            switch (command.Kind)
            {
                case LauncherAllGroupsFolderMutationKind.Create:
                    if (!flat.Contains(subject)
                        || !flat.Contains(target)
                        || !ShortcutFolderService.CanCreateFolderFrom(subject, target))
                    {
                        return;
                    }

                    requestValid = true;
                    mutated = AllGroupsShortcutViewService.TryCreateOwnedFolder(
                        data.AppConfig.AllGroupsIconList,
                        flat,
                        subject,
                        target,
                        command.InsertIndex,
                        out _);
                    break;

                case LauncherAllGroupsFolderMutationKind.MoveInto:
                    if (!flat.Contains(subject)
                        || !flat.Contains(target)
                        || !AllGroupsShortcutViewService.IsAllGroupsRootItem(
                            data.AppConfig.AllGroupsIconList,
                            target)
                        || !ShortcutFolderService.CanMoveIntoFolder(subject, target))
                    {
                        return;
                    }

                    requestValid = true;
                    mutated = AllGroupsShortcutViewService.TryMoveIntoOwnedFolder(
                        data.AppConfig.AllGroupsIconList,
                        flat,
                        subject,
                        target);
                    break;

                case LauncherAllGroupsFolderMutationKind.MoveOut:
                    if (!flat.Contains(target)
                        || !AllGroupsShortcutViewService.IsAllGroupsRootItem(
                            data.AppConfig.AllGroupsIconList,
                            target)
                        || target.IsFolder != true
                        || !target.FolderItems.Contains(subject))
                    {
                        return;
                    }

                    requestValid = true;
                    mutated = AllGroupsShortcutViewService.TryMoveOutOfOwnedFolder(
                        data.AppConfig.AllGroupsIconList,
                        flat,
                        target,
                        subject,
                        command.InsertIndex);
                    break;

                default:
                    return;
            }

            if (mutated)
            {
                changed = true;
                changed |= SetAllGroupsFlatOrderKeys(data, flat);
            }
        });

        if (!requestValid)
        {
            return LauncherCommandResult.Failed(
                "All Groups 文件夹命令引用了无效或已过期的快捷方式投影。");
        }
        if (changed)
        {
            _saveService.SaveDebounced();
        }
        return new LauncherCommandResult(true, changed);
    }

    private static bool TryResolveRootOrder(
        MenuInfo group,
        IReadOnlyList<string> ids,
        out List<IconInfo> ordered)
    {
        var roots = group.IconList
            .Where(icon => icon != null
                && !icon.IsAddShortcut_NoWrite
                && !string.IsNullOrWhiteSpace(icon.IconId))
            .GroupBy(icon => icon.IconId, StringComparer.Ordinal)
            .ToDictionary(items => items.Key, items => items.First(), StringComparer.Ordinal);
        ordered = new List<IconInfo>(ids.Count);
        foreach (string id in ids)
        {
            if (!roots.TryGetValue(id, out IconInfo? icon))
            {
                ordered.Clear();
                return false;
            }
            ordered.Add(icon);
        }
        return true;
    }

    private static bool TryResolveAllGroupsOrder(
        AppData data,
        IReadOnlyList<string> ids,
        out List<IconInfo> flat)
    {
        flat = new List<IconInfo>(ids.Count);
        foreach (string id in ids)
        {
            IconInfo? icon = FindLayoutIcon(data, id);
            if (icon == null
                || !AllGroupsShortcutViewService.IsVisibleFlatItem(
                    data.MenuList,
                    data.AppConfig.AllGroupsIconList,
                    icon))
            {
                flat.Clear();
                return false;
            }
            flat.Add(icon);
        }
        return true;
    }

    private static bool TryResolveExactAllGroupsOrder(
        AppData data,
        IReadOnlyList<string> ids,
        out List<IconInfo> flat)
    {
        if (!TryResolveAllGroupsOrder(data, ids, out flat))
        {
            return false;
        }

        List<IconInfo> visible = AllGroupsShortcutViewService.BuildDefaultFlatOrder(
            data.MenuList,
            data.AppConfig.AllGroupsIconList);
        return visible.Count == flat.Count
            && new HashSet<IconInfo>(visible).SetEquals(flat);
    }

    private static bool SetAllGroupsFlatOrderKeys(AppData data, IReadOnlyList<IconInfo> flat)
    {
        List<string> keys = flat
            .Select(icon => "id:" + icon.IconId)
            .ToList();
        if ((data.AppConfig.AllGroupsFlatIconOrderKeys ?? new List<string>())
            .SequenceEqual(keys, StringComparer.Ordinal))
        {
            return false;
        }

        data.AppConfig.AllGroupsFlatIconOrderKeys = keys;
        return true;
    }

    private static bool ApplyVisibleOrder(
        ObservableCollection<IconInfo> collection,
        IList<IconInfo> ordered)
    {
        IconInfo[] before = collection.ToArray();
        AllGroupsShortcutViewService.ApplyVisibleOrderPreservingHiddenItems(collection, ordered);
        return !before.SequenceEqual(collection);
    }

    private static bool IsValidGridPlacement(LauncherGridPlacement placement) =>
        placement.Column >= 0 && placement.Row >= 0
        || placement.Column == -1 && placement.Row == -1;

    private LauncherCommandResult UpdateShortcut(string shortcutId, Func<IconInfo, bool> update)
    {
        bool found = false;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            IconInfo? shortcut = FindIcon(data, shortcutId);
            if (shortcut == null)
            {
                return;
            }

            found = true;
            changed = update(shortcut);
        });

        if (!found)
        {
            return LauncherCommandResult.Failed("快捷方式不存在。");
        }

        if (changed)
        {
            _saveService.SaveDebounced();
        }

        return new LauncherCommandResult(true, changed);
    }

    private static bool AssignIfDifferent<T>(T current, T replacement, Action<T> assign)
    {
        if (EqualityComparer<T>.Default.Equals(current, replacement))
        {
            return false;
        }

        assign(replacement);
        return true;
    }

    private static bool ByteArraysEqual(byte[]? left, byte[]? right) =>
        ReferenceEquals(left, right)
        || left != null && right != null && left.AsSpan().SequenceEqual(right);

    private static LauncherDropRequest? BuildRequest(AppData data, LauncherMutationCommand command)
    {
        IconInfo? dragged = FindIcon(data, command.DraggedShortcutId);
        IconInfo? target = FindIcon(data, command.TargetShortcutId);
        return command.Kind switch
        {
            LauncherMutationKind.CreateFolder when dragged != null && target != null =>
                LauncherDropRequest.CreateFolder(dragged, target, command.InsertIndex),
            LauncherMutationKind.MoveIntoFolder when dragged != null && target != null =>
                LauncherDropRequest.MoveIntoFolder(dragged, target),
            LauncherMutationKind.Reorder when dragged != null =>
                LauncherDropRequest.Reorder(dragged, command.InsertIndex),
            LauncherMutationKind.MoveToGroup => BuildMoveToGroupRequest(data, command),
            _ => null
        };
    }

    private static LauncherDropRequest? BuildMoveToGroupRequest(
        AppData data,
        LauncherMutationCommand command)
    {
        MenuInfo? targetMenu = data.MenuList.FirstOrDefault(menu =>
            string.Equals(menu.MenuId, command.TargetGroupId, StringComparison.Ordinal));
        if (targetMenu == null || command.ShortcutIds == null)
        {
            return null;
        }

        var icons = command.ShortcutIds
            .Select(id => FindIcon(data, id))
            .ToList();
        if (icons.Any(icon => icon == null))
        {
            return null;
        }

        return LauncherDropRequest.MoveToGroup(
            icons.Cast<IconInfo>().ToList(),
            targetMenu,
            command.InsertIndex);
    }

    private static IconInfo? FindIcon(AppData data, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return EnumerateIconTree(data.AppConfig.AllGroupsIconList)
            .Concat(ShortcutFolderTreeService.EnumerateIcons(data.MenuList))
            .FirstOrDefault(icon => string.Equals(icon.IconId, id, StringComparison.Ordinal));
    }

    private static IconInfo? FindLayoutIcon(AppData data, string id) =>
        EnumerateIconTree(data.AppConfig.AllGroupsIconList)
            .Concat(ShortcutFolderTreeService.EnumerateIcons(data.MenuList))
            .FirstOrDefault(icon => string.Equals(icon.IconId, id, StringComparison.Ordinal));

    private static IEnumerable<IconInfo> EnumerateIconTree(IEnumerable<IconInfo>? icons)
    {
        if (icons == null)
        {
            yield break;
        }

        foreach (IconInfo icon in icons)
        {
            if (icon == null)
            {
                continue;
            }

            yield return icon;
            if (icon.IsFolder)
            {
                foreach (IconInfo child in EnumerateIconTree(icon.FolderItems))
                {
                    yield return child;
                }
            }
        }
    }
}
