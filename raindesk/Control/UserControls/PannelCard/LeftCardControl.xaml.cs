using DraggAnimatedPanelExample;
using raindesk.Constant;
using raindesk.Controls;
using raindesk.Control.Other;
using raindesk.Control.Windows;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Features.Workspace.Contracts;
using raindesk.Features.Launcher.Presentation;
using raindesk.Services;
using raindesk.Services.Interfaces;
using System;

using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace raindesk.Control.UserControls.PannelCard
{
    /// <summary>
    /// LeftCardControl.xaml 的交互逻辑
    /// </summary>
    public partial class LeftCardControl : UserControl
    {
        private int menuSelectIndexTemp = -1;
        private readonly IAppDataProvider _dataProvider;
        private readonly IWorkspaceCommandHandler _workspaceCommandHandler;
        private readonly ISaveService _saveService;
        private readonly LauncherBatchModeState _batchMode;
        private AppData appData => _dataProvider.Current;
        private SolidColorBrush bac = new SolidColorBrush(ColorHelper.FromArgb(255, 255, 255, 255));

        // 复用 SolidColorBrush 避免重复创建
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        private static readonly SolidColorBrush BlackBrush = new SolidColorBrush(Microsoft.UI.Colors.Black);



        public LeftCardControl()
        {
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _workspaceCommandHandler = AppServices.Get<IWorkspaceCommandHandler>();
            _saveService = AppServices.Get<ISaveService>();
            _batchMode = AppServices.Get<LauncherBatchModeState>();
            this.InitializeComponent();
            bac.Opacity = 0.6;

            this.Loaded += (s, e) =>
            {
                SelectLastMenu();
            };
        }

        private void MenuListBox_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue || args.Item is not MenuInfo menu)
            {
                return;
            }

            if (args.Phase == 0)
            {
                args.RegisterUpdateCallback(MenuListBox_ContainerContentChanging);
                return;
            }

            if (args.ItemContainer?.ContentTemplateRoot is DependencyObject root)
            {
                var iconPath = MenuGeometryHelper.FindDescendantByName<Microsoft.UI.Xaml.Shapes.Path>(root, "MenuIconPath");
                MenuGeometryHelper.ApplyToPath(iconPath, menu.MenuGeometry);
            }
        }

        internal void RefreshMenuIcon(MenuInfo menu)
        {
            if (MenuListBox.ContainerFromItem(menu) is not ListViewItem item
                || item.ContentTemplateRoot is not DependencyObject root)
            {
                return;
            }

            var iconPath = MenuGeometryHelper.FindDescendantByName<Microsoft.UI.Xaml.Shapes.Path>(root, "MenuIconPath");
            MenuGeometryHelper.ApplyToPath(iconPath, menu.MenuGeometry);
        }

        private void SelectLastMenu()
        {
            if (appData.MenuList.Count == 0)
            {
                WorkspaceCommandResult created = _workspaceCommandHandler.Create(
                    new CreateWorkspaceGroupCommand("NewMenu"));
                if (!created.Success)
                {
                    GrowlService.Warning(created.Error ?? "创建默认菜单失败。");
                    return;
                }
            }

            if (appData.AppConfig.SelectedMenuIndex >= appData.MenuList.Count || appData.AppConfig.SelectedMenuIndex == -1)
            {
                MenuListBox.SelectedIndex = 0;
                _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(0));
            }
            else
            {
                MenuListBox.SelectedIndex = appData.AppConfig.SelectedMenuIndex;
                _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(appData.AppConfig.SelectedMenuIndex));
            }
            RefreshMenuSelectionState();
        }

        raindesk.Util.DelegateCommand<int[]> _swap;

        public raindesk.Util.DelegateCommand<int[]> SwapCommand
        {
            get
            {
                if (_swap == null)
                    _swap = new raindesk.Util.DelegateCommand<int[]>(
                        (indexes) =>
                        {
                            int fromS = indexes[0];
                            int to = indexes[1];
                            if (fromS < 0 || fromS >= appData.MenuList.Count
                                || to < 0 || to >= appData.MenuList.Count)
                            {
                                return;
                            }

                            var orderedIds = appData.MenuList.Select(menu => menu.MenuId).ToList();
                            string draggedId = orderedIds[fromS];
                            orderedIds.RemoveAt(fromS);
                            orderedIds.Insert(to, draggedId);
                            WorkspaceCommandResult result = _workspaceCommandHandler.Reorder(
                                new ReorderWorkspaceGroupsCommand(orderedIds));
                            if (result.Success)
                            {
                                MenuListBox.SelectedIndex = to;
                            }
                        }
                    );
                return _swap;
            }
        }


        /// <summary>
        /// 当修改菜单元素可见时 设置原菜单为不可见 并且不可选中
        /// 修改菜单元素不可见时  原菜单可见 并 选中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuWhenVisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            StackPanel sp = sender as StackPanel;

            // WinUI 3: TemplatedParent access differs
            if (sp.Visibility == Visibility.Collapsed)
            {
                if (MenuListBox.SelectedIndex != -1)
                {
                    menuSelectIndexTemp = MenuListBox.SelectedIndex;
                    MenuListBox.SelectedIndex = -1;
                }
                else
                {
                    MenuListBox.SelectedIndex = menuSelectIndexTemp;
                }
            }
            else
            {
                // Re-register events when visible
            }
        }

        #region 设置菜单触发事件
        private void Lbi_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ListViewItem lbi = sender as ListViewItem;
            lbi.Background = TransparentBrush;
        }

        private void Lbi_Unselected(object sender, RoutedEventArgs e)
        {
            //添加Leave效果
            ListViewItem lbi = sender as ListViewItem;
            lbi.Background = TransparentBrush;
            lbi.PointerExited += Lbi_PointerExited;
        }

        private void Lbi_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                ListViewItem lbi = sender as ListViewItem;
                lbi.PointerExited -= Lbi_PointerExited;
                lbi.Background = bac;
                lbi.Foreground = BlackBrush;
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "Lbi_Selected 事件处理失败!"); }

        }

        private void Lbi_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ListViewItem lbi = sender as ListViewItem;
            lbi.Background = TransparentBrush;
        }
        #endregion

        /// <summary>
        /// 新建菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateMenu(object sender, RoutedEventArgs e)
        {
            WorkspaceCommandResult result = _workspaceCommandHandler.Create(
                new CreateWorkspaceGroupCommand("NewMenu"));
            MenuInfo? info = result.EntityId == null
                ? null
                : appData.MenuList.FirstOrDefault(menu => menu.MenuId == result.EntityId);
            if (!result.Success || info == null)
            {
                GrowlService.Warning(result.Error ?? "新建菜单失败。");
                return;
            }

            MenuListBox.SelectedIndex = appData.MenuList.IndexOf(info);
            _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(MenuListBox.SelectedIndex));
            RefreshMenuSelectionState();
        }

        /// <summary>
        /// 创建实时文件菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CreateLinkMenu(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                var hwnd = WindowNative.GetWindowHandle(MainWindow.mainWindow);
                InitializeWithWindow.Initialize(picker, hwnd);
                picker.SuggestedStartLocation = PickerLocationId.Desktop;
                picker.FileTypeFilter.Add("*");

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    string path = folder.Path;
                    WorkspaceCommandResult result = _workspaceCommandHandler.Create(
                        new CreateWorkspaceGroupCommand(
                            Path.GetFileNameWithoutExtension(path),
                            WorkspaceGroupKind.LinkedFolder,
                            path));
                    MenuInfo? menuInfo = result.EntityId == null
                        ? null
                        : appData.MenuList.FirstOrDefault(menu => menu.MenuId == result.EntityId);
                    if (!result.Success || menuInfo == null)
                    {
                        GrowlService.Warning(result.Error ?? "新建关联菜单失败。");
                        return;
                    }

                    MenuListBox.SelectedIndex = appData.MenuList.IndexOf(menuInfo);
                    _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(MenuListBox.SelectedIndex));
                    RefreshMenuSelectionState();
                    GrowlService.Success("菜单关联成功, 加载列表中, 稍后重新进入此菜单可查看列表!", "MainWindowGrowl");
                    FileWatcher.LinkMenuWatcher(menuInfo);

                    // 使用 async/await 替代裸 Thread
                    _ = System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(menuInfo.LinkPath);
                            FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();

                            ObservableCollection<IconInfo> iconList = new ObservableCollection<IconInfo>();
                            foreach (FileSystemInfo fileInfo in fileInfos)
                            {
                                IconInfo iconInfo = CommonCode.GetIconInfoByPath_NoWrite(fileInfo.FullName);
                                iconList.Add(iconInfo);
                            }
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                // R6 已知例外：关联文件夹内容加载
                                menuInfo.IconList = iconList;
                            });
                        }
                        catch (Exception ex)
                        {
                            LogUtil.WriteErrorLog(ex, "加载关联菜单文件列表失败");
                        }
                    });

                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "新建关联菜单失败!");
                GrowlService.WarningGlobal("新建关联菜单失败!");
            }
        }


        /// <summary>
        /// 重命名菜单 将textbox 设置为可见
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RenameMenu(object sender, RoutedEventArgs e)
        {
            RunTimeStatus.IS_MENU_EDIT = true;
            MenuInfo menuInfo = ((MenuFlyoutItem)sender).Tag as MenuInfo;
            if (menuInfo != null)
                menuInfo.MenuEdit = Visibility.Visible;
        }

        /// <summary>
        /// 删除菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeleteMenu(object sender, RoutedEventArgs e)
        {

            MenuInfo menuInfo = ((MenuFlyoutItem)sender).Tag as MenuInfo;
            if (menuInfo.IconList != null && menuInfo.IconList.Count > 0)
            {
                bool confirmed = await GrowlService.AskAsync("确认删除此菜单吗?", "MainWindowAskGrowl");
                if (confirmed)
                {
                    DeleteMenu(menuInfo);
                }
            } else
            {
                DeleteMenu(menuInfo);
            }
        }

        private void DeleteMenu(MenuInfo menuInfo)
        {
            if (appData.MenuList.Count == 1)
            {
                //如果删除以后没有菜单的话 先创建一个
                CreateMenu(null, null);
            }
            int index = appData.MenuList.IndexOf(menuInfo);
            index = Math.Max(0, index - 1);

            WorkspaceCommandResult result = _workspaceCommandHandler.Delete(
                new DeleteWorkspaceGroupCommand(menuInfo.MenuId));
            if (!result.Success)
            {
                GrowlService.Warning(result.Error ?? "删除菜单失败。");
                return;
            }

            // 选中下一个菜单
            MenuListBox.SelectedIndex = index;
            _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(MenuListBox.SelectedIndex));
            RefreshMenuSelectionState();
        }

        /// <summary>
        /// 编辑菜单失焦或者敲下Enter键时保存修改后的菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LostFocusOrEnterDown(object sender, object e)
        {
            bool done = true;
            TextBox menuBox = null;
            if (e is KeyRoutedEventArgs eKey)
            {
                if (eKey.Key == global::Windows.System.VirtualKey.Enter)
                {
                    menuBox = ((TextBox)sender);
                }
                else
                {
                    done = false;
                }
            }
            else if (e is RoutedEventArgs)
            {
                menuBox = ((TextBox)sender);
            }

            if (done)
            {
                if (menuBox != null)
                {
                    MenuInfo menuInfo = menuBox.Tag as MenuInfo;
                    string text = menuBox.Text;
                    WorkspaceCommandResult result = _workspaceCommandHandler.Rename(
                        new RenameWorkspaceGroupCommand(menuInfo.MenuId, text));
                    if (!result.Success)
                    {
                        GrowlService.Warning(result.Error ?? "菜单重命名失败。");
                    }
                    menuInfo.MenuEdit = Visibility.Collapsed;
                }
                RunTimeStatus.IS_MENU_EDIT = false;
                //为了解决无法修改菜单的问题
                MainWindow.mainWindow.SearchBox.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                MenuListBox.SelectedIndex = menuSelectIndexTemp;
            }
        }

        /// <summary>
        /// 当修改菜单元素可见时 设置全选并获得焦点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuEditWhenVisibilityChanged(object sender, VisibilityChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            MenuInfo mi = box.Tag as MenuInfo;
            if (box.Visibility == Visibility.Visible)
            {
                box.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
                box.SelectAll();
            }
        }

        /// <summary>
        /// 修改菜单图标
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditMenuGeometry(object sender, RoutedEventArgs e)
        {
            MenuInfo menuInfo = ((MenuFlyoutItem)sender).Tag as MenuInfo;
            IconfontWindow.Show(SvgToGeometry.GetIconfonts(), menuInfo);
        }

        private void Menu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RunTimeStatus.IS_MENU_EDIT) return;
            int selectedIndex = MenuListBox.SelectedIndex;
            if (selectedIndex != -1 && selectedIndex < appData.MenuList.Count
                && appData.MenuList[selectedIndex].IsEncrypt)
            {
                _workspaceCommandHandler.SelectAll(new SelectAllGroupsCommand());
                RunTimeStatus.SHOW_MENU_PASSWORDBOX = true;
                var rightCard = MainWindow.mainWindow?.RightCard;
                var passwordDialog = rightCard?.EnsurePasswordDialog();
                if (passwordDialog != null)
                {
                    passwordDialog.Title.Text = "输入密码";
                    passwordDialog.type = PasswordType.INPUT;
                    passwordDialog._pendingEncryptedGroupIndex = selectedIndex;
                    passwordDialog.Visibility = Visibility.Visible;
                }
                return;
            }

            _workspaceCommandHandler.Select(new SelectWorkspaceGroupCommand(selectedIndex));
            RefreshMenuSelectionState();
            var rc = MainWindow.mainWindow?.RightCard;
            if (rc == null) return;

            var shortcutPanel = rc.EnsureWrapUFG();
            if (appData.AppConfig.ItemSpradeAnimation && shortcutPanel != null)
            {
                shortcutPanel.Visibility = Visibility.Collapsed;
            }

            if (selectedIndex != -1 && selectedIndex < appData.MenuList.Count)
            {
                var passwordDialog = rc.EnsurePasswordDialog();
                if (passwordDialog != null)
                {
                    passwordDialog.Visibility = Visibility.Collapsed;
                }
            }

            if (shortcutPanel != null)
            {
                shortcutPanel.Visibility = Visibility.Visible;
            }
        }

        private void RefreshMenuSelectionState()
        {
            for (int i = 0; i < appData.MenuList.Count; i++)
            {
                appData.MenuList[i].IsSelected_NoWrite = i == MenuListBox.SelectedIndex;
            }
        }


        /// <summary>
        /// 鼠标悬停切换菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Menu_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (appData.AppConfig.HoverMenu && !RunTimeStatus.IS_MENU_EDIT)
            {
                // 使用 async/await 替代裸 Thread
                await System.Threading.Tasks.Task.Delay(200);
                ListViewItem lbi = sender as ListViewItem;
                // WinUI 3: Check if pointer is still over the element
                int index = MenuListBox.IndexFromContainer(lbi);
                MenuListBox.SelectedIndex = index;
                _batchMode.SetEnabled(false);
            }
        }

        /// <summary>
        /// 点击菜单后  隐藏搜索框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListBoxItem_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (RunTimeStatus.SEARCH_BOX_SHOW)
            {
                MainWindow.mainWindow.HidedSearchBox();
            }

            ListViewItem lbi = sender as ListViewItem;
            MenuInfo mi = lbi.DataContext as MenuInfo;
            int index = MenuListBox.Items.IndexOf(mi);

            //点击已选中的菜单时 手动刷新图标列表 (SelectionChanged不会触发)
            if (index == MenuListBox.SelectedIndex)
            {
                Menu_SelectionChanged(MenuListBox, null);
            }
            else
            {
                MenuListBox.SelectedIndex = index;
            }

            _batchMode.SetEnabled(false);
        }


        /// <summary>
        /// 隐藏搜索框
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (RunTimeStatus.SEARCH_BOX_SHOW)
            {
                MainWindow.mainWindow.HidedSearchBox();
            }
        }

        private void Menu_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (RunTimeStatus.IS_MENU_EDIT) return;

            // WinUI 3: ScrollViewer access differs
            // Simplified wheel handling
        }


        /// <summary>
        /// 拖动移动图标到指定菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MyCard_Drop(object sender, DragEventArgs e)
        {
            MyPoptip.IsOpen = false;
            e.AcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.Handled = true;

            if (!e.DataView.Contains(global::Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                return;
            }

            MenuInfo? targetMenu = (sender as FrameworkElement)?.DataContext as MenuInfo;
            if (targetMenu?.IconList == null)
            {
                return;
            }

            var items = await e.DataView.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (item is global::Windows.Storage.StorageFile file)
                {
                    targetMenu.IconList.Add(CommonCode.GetIconInfoByPath(file.Path));
                }
            }

            _saveService.SaveDebounced();
        }

        private void MyCard_DragEnter(object sender, DragEventArgs e)
        {
            MyPoptipContent.Text = "移动至: 菜单";
            MyPoptip.IsOpen = true;
        }

        private void MyCard_DragLeave(object sender, DragEventArgs e)
        {
            MyPoptip.IsOpen = false;
        }

        private void EncryptMenu(object sender, RoutedEventArgs e)
        {
            MenuInfo menuInfo = ((MenuFlyoutItem)sender).Tag as MenuInfo;
            var rightCard = MainWindow.mainWindow?.RightCard;
            var passwordDialog = rightCard?.EnsurePasswordDialog();
            if (rightCard == null || passwordDialog == null) return;

            if (menuInfo.IsEncrypt)
            {
                passwordDialog.menuInfo = menuInfo;
                passwordDialog.Title.Text = "输入密码";
                passwordDialog.type = PasswordType.CANCEL;
                RunTimeStatus.SHOW_MENU_PASSWORDBOX = true;
                passwordDialog.Visibility = Visibility.Visible;
                passwordDialog.SetFocus();
            }
            else
            {
                if (string.IsNullOrEmpty(appData.AppConfig.MenuPassword))
                {
                    passwordDialog.menuInfo = menuInfo;
                    passwordDialog.Title.Text = "设置新密码";
                    passwordDialog.type = PasswordType.CREATE;
                    RunTimeStatus.SHOW_MENU_PASSWORDBOX = true;
                    passwordDialog.Visibility = Visibility.Visible;
                }
                else
                {
                    _workspaceCommandHandler.SetEncryption(new SetGroupEncryptionCommand(menuInfo.MenuId, true));
                    GrowlService.Success(menuInfo.MenuName + " 已加密!", "MainWindowGrowl");
                }
            }
        }

        private void AlterPassword(object sender, RoutedEventArgs e)
        {
            var passwordDialog = MainWindow.mainWindow?.RightCard?.EnsurePasswordDialog();
            if (passwordDialog == null) return;

            passwordDialog.Title.Text = "输入旧密码";
            passwordDialog.type = PasswordType.ALTER;
            passwordDialog.Visibility = Visibility.Visible;
            passwordDialog.SetFocus();
        }

        /// <summary>
        /// 右键点击进行处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            RunTimeStatus.SHOW_RIGHT_BTN_MENU = true;
            // 使用 async/await 替代裸 Thread
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(50);
                RunTimeStatus.SHOW_RIGHT_BTN_MENU = false;
            });

            //在没有设置密码的情况下不弹出修改密码菜单
            if (string.IsNullOrEmpty(appData.AppConfig.MenuPassword))
            {
                AlterPW1.Visibility = Visibility.Collapsed;
            }
            else
            {
                AlterPW1.Visibility = Visibility.Visible;
            }
        }

    }
}
