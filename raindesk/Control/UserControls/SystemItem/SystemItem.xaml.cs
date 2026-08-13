using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;
using raindesk.Services;
using raindesk.Services.Interfaces;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace raindesk.Control.UserControls.SystemItem
{
    /// <summary>
    /// SystemItem.xaml 的交互逻辑
    /// </summary>
    public partial class SystemItem : UserControl
    {
        private readonly IAppDataProvider _dataProvider;
        private readonly ILauncherCommandHandler _launcherCommands;

        public SystemItem()
        {
            this.InitializeComponent();
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _launcherCommands = AppServices.Get<ILauncherCommandHandler>();
        }
        private void Icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                IconInfo thisInfo = (sender as FrameworkElement)?.Tag as IconInfo;
                if (thisInfo == null) return;

                // 检查 SelectedMenuIndex 是否有效
                (int selectedIndex, int menuCount) = _dataProvider.Read(data =>
                    (data.AppConfig.SelectedMenuIndex, data.MenuList.Count));
                if (selectedIndex < 0 || selectedIndex >= menuCount)
                {
                    if (menuCount > 0)
                    {
                        selectedIndex = 0;
                    }
                    else
                    {
                        // No menu yet — prompt user to create one first.
                        var dialog = new ContentDialog
                        {
                            Title = "提示",
                            Content = "请先创建一个菜单！",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        _ = dialog.ShowAsync();
                        return;
                    }
                }

                string? targetGroupId = _dataProvider.Read(data => data.MenuList[selectedIndex].MenuId);
                if (string.IsNullOrWhiteSpace(targetGroupId))
                {
                    throw new InvalidOperationException("当前分组缺少稳定标识。");
                }

                LauncherCommandResult result = _launcherCommands.Add(new AddLauncherShortcutCommand(
                    targetGroupId,
                    LauncherSystemCandidatePresentationMapper.ToDraft(thisInfo)));
                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Error ?? "添加快捷方式失败。");
                }
            }
            catch (System.Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "添加系统项目失败");
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = "添加系统项目失败: " + ex.Message,
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }
    }
}
