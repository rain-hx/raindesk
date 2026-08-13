using rainspace.Features.Launcher.Contracts;

namespace rainspace.Features.Launcher.Application;

/// <summary>根据 Launcher 快捷方式特征决定上下文菜单能力，不依赖 WinUI 控件。</summary>
public static class LauncherContextMenuPolicy
{
    private static readonly LauncherContextMenuPlan Empty = new(Array.Empty<LauncherContextMenuAction>());

    public static LauncherContextMenuPlan Resolve(LauncherContextMenuRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.IsPlaceholder || string.IsNullOrWhiteSpace(request.ShortcutId))
        {
            return Empty;
        }

        if (request.IsFolder)
        {
            return new LauncherContextMenuPlan(new[]
            {
                LauncherContextMenuAction.OpenFolder,
                LauncherContextMenuAction.ResizeFolder,
                LauncherContextMenuAction.Rename,
                LauncherContextMenuAction.Remove
            });
        }

        var actions = new List<LauncherContextMenuAction>
        {
            LauncherContextMenuAction.RunAsAdministrator,
            LauncherContextMenuAction.EditProperties,
            LauncherContextMenuAction.Rename,
            LauncherContextMenuAction.Remove
        };
        if (request.HasFileLocation)
        {
            actions.Add(LauncherContextMenuAction.ShowInFileExplorer);
        }

        return new LauncherContextMenuPlan(actions);
    }
}
