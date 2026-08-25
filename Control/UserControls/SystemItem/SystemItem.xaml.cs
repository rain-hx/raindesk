using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Controls;
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

        /// <summary>
        /// 开始菜单页：点击项目后先确认目标分组并防止重复添加。
        /// </summary>
        public bool RequireAddConfirmation { get; set; }

        public SystemItem()
        {
            this.InitializeComponent();
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _launcherCommands = AppServices.Get<ILauncherCommandHandler>();
        }

        private async void Icon_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                IconInfo thisInfo = (sender as FrameworkElement)?.Tag as IconInfo;
                if (thisInfo == null) return;

                if (RequireAddConfirmation)
                {
                    await AddStartMenuItemWithConfirmationAsync(thisInfo);
                    return;
                }

                AddToCurrentGroup(thisInfo);
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

        private void AddToCurrentGroup(IconInfo icon)
        {
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
                    ShowPromptDialog("请先创建一个分组！");
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
                LauncherSystemCandidatePresentationMapper.ToDraft(icon)));
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Error ?? "添加快捷方式失败。");
            }
        }

        private async global::System.Threading.Tasks.Task AddStartMenuItemWithConfirmationAsync(IconInfo icon)
        {
            IReadOnlyList<MenuInfo> groups = _dataProvider.Read(data => data.MenuList.ToList());
            if (groups.Count == 0)
            {
                ShowPromptDialog("请先创建一个分组！");
                return;
            }

            int currentIndex = _dataProvider.Read(data =>
            {
                int index = data.AppConfig.SelectedMenuIndex;
                return index >= 0 && index < groups.Count ? index : 0;
            });

            var combo = new ComboBox
            {
                ItemsSource = groups,
                DisplayMemberPath = nameof(MenuInfo.MenuName),
                SelectedIndex = currentIndex,
                MinWidth = 220,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var content = new StackPanel { Spacing = 10 };
            content.Children.Add(new TextBlock
            {
                Text = $"将「{icon.Name_NoWrite}」添加到分组：",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14
            });
            content.Children.Add(combo);

            var dialog = new ContentDialog
            {
                Title = "添加快捷方式",
                Content = content,
                PrimaryButtonText = "添加",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary || combo.SelectedItem is not MenuInfo targetGroup)
            {
                return;
            }

            if (ContainsDuplicate(targetGroup, icon))
            {
                GrowlService.Warning($"「{icon.Name_NoWrite}」已存在于分组「{targetGroup.MenuName}」，无需重复添加。");
                return;
            }

            LauncherCommandResult addResult = _launcherCommands.Add(new AddLauncherShortcutCommand(
                targetGroup.MenuId,
                LauncherSystemCandidatePresentationMapper.ToDraft(icon)));
            if (!addResult.Success)
            {
                throw new InvalidOperationException(addResult.Error ?? "添加快捷方式失败。");
            }

            GrowlService.Success($"已添加「{icon.Name_NoWrite}」到「{targetGroup.MenuName}」。");
        }

        private static bool ContainsDuplicate(MenuInfo group, IconInfo candidate)
        {
            if (group.IconList == null)
            {
                return false;
            }

            foreach (IconInfo icon in group.IconList)
            {
                if (icon.IsAddShortcut_NoWrite)
                {
                    continue;
                }

                if (string.Equals(icon.Name, candidate.Name_NoWrite, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(icon.Path_NoWrite)
                    && !string.IsNullOrWhiteSpace(candidate.Path_NoWrite)
                    && string.Equals(icon.Path_NoWrite, candidate.Path_NoWrite, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowPromptDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "提示",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            _ = dialog.ShowAsync();
        }
    }
}
