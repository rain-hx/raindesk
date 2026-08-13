using rainspace.Features.Launcher.Contracts;
using rainspace.Constant;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Features.Launcher.Infrastructure;

/// <summary>通过当前 Store 查找稳定 ID，并适配既有 Windows Shell 启动能力。</summary>
public sealed class WindowsLauncherActivationPort : ILauncherActivationPort
{
    private readonly IAppDataProvider _dataProvider;

    public WindowsLauncherActivationPort(IAppDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public LauncherActivationResult Activate(LauncherActivationCommand command)
    {
        if (command == null || string.IsNullOrWhiteSpace(command.ShortcutId))
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.InvalidRequest,
                "快捷方式 ID 不能为空。");
        }

        IconInfo? icon = ShortcutFolderTreeService
            .EnumerateIcons(_dataProvider.Current.MenuList)
            .FirstOrDefault(candidate => string.Equals(
                candidate.IconId,
                command.ShortcutId,
                StringComparison.Ordinal));
        if (icon == null)
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.ShortcutNotFound,
                "快捷方式不存在。");
        }
        if (icon.IsFolder || icon.IsAddShortcut_NoWrite || icon.IsDragYieldPlaceholder_NoWrite)
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.NotLaunchable,
                "该项目不能作为快捷方式激活。");
        }

        IconStartType startType = command.Mode switch
        {
            LauncherActivationMode.Administrator => IconStartType.ADMIN_STARTUP,
            LauncherActivationMode.ShowInFileExplorer => IconStartType.SHOW_IN_EXPLORE,
            _ => icon.AdminStartUp
                ? IconStartType.ADMIN_STARTUP
                : IconStartType.DEFAULT_STARTUP
        };
        try
        {
            ProcessUtil.StartIconApp(icon, startType);
            return LauncherActivationResult.Activated;
        }
        catch (Exception ex)
        {
            return LauncherActivationResult.Failed(
                LauncherActivationErrorCode.ActivationFailed,
                ex.Message);
        }
    }
}
