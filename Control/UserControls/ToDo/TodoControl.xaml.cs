using raindesk.Constant;
using raindesk.Control.Windows;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Controls;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using raindesk.Services;
using raindesk.Services.Interfaces;

namespace raindesk.Control.UserControls.Backlog
{

    public enum ToDoType
    {
        HISTORY = 1,
        NEW = 2
    }
    /// <summary>
    /// TodoControl.xaml 的交互逻辑
    /// </summary>
    public partial class TodoControl : UserControl
    {
        private readonly IAppDataProvider _dataProvider;
        private readonly IToDoService _todoService;
        public ToDoType type;
        public TodoControl()
        {
            _dataProvider = AppServices.Get<IAppDataProvider>();
            _todoService = AppServices.Get<IToDoService>();
            this.InitializeComponent();
        }

        private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
        {
            ToDoInfo? info = BacklogListView.SelectedItem as ToDoInfo;
            if (info == null || string.IsNullOrWhiteSpace(info.Id))
            {
                return;
            }

            bool confirmed = await GrowlService.AskAsync("确认删除吗?", "DeleteConfirm");
            if (confirmed)
            {
                _todoService.Delete(info.Id);
            }
        }

        private void DetailMenu_Click(object sender, RoutedEventArgs e)
        {
            ToDoInfo info = BacklogListView.SelectedItem as ToDoInfo;
            ToDoInfoWindow.ShowDetail(info);
        }

        /// <summary>
        /// 打开右键菜单
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BacklogListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var listView = sender as ListView;
            var originalSource = e.OriginalSource as FrameworkElement;
            ToDoInfo info = originalSource?.DataContext as ToDoInfo;
            if (info == null) return;

            int index;
            if (type == ToDoType.NEW)
            {
                index = _dataProvider.Current.ToDoList.IndexOf(info);
            }
            else
            {
                index = _dataProvider.Current.HiToDoList.IndexOf(info);
            }
            BacklogListView.SelectedIndex = index;
            Menu.ShowAt(listView, e.GetPosition(listView));
        }

        /// <summary>
        /// 选中时颜色变化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Row_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                grid.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(230, 91, 192, 222));
            }
        }

        private void Row_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                grid.Background = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = 0.6 };
            }
        }
    }
}
