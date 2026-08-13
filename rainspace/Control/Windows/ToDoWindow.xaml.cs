using rainspace.Control.UserControls.Backlog;
using rainspace.Interface;
using rainspace.Util;
using rainspace.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Input;
using rainspace.Services;
using rainspace.Services.Interfaces;

namespace rainspace.Control.Windows
{
    /// <summary>
    /// BacklogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ToDoWindow : Window, IWindowCommon
    {
        private static TodoControl backlog = new TodoControl();
        private readonly IAppDataProvider _dataProvider = AppServices.Get<IAppDataProvider>();
        private AppData appData => _dataProvider.Current;
        private ToDoWindow()
        {
            InitializeComponent();
            rainspace.Services.ThemeService.RegisterWindow(this);
            RightCard.Child = backlog;
            backlog.BacklogListView.ItemsSource = appData.ToDoList;
            backlog.type = ToDoType.NEW;
            this.Topmost = true;
            // WinUI 3: Center on screen and disable resize
            WindowDragHelper.CenterOnScreen(this);
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);
            if (backlog.BacklogListView.Items.Count > 0)
            {
                backlog.NoData.Visibility = Visibility.Collapsed;
                backlog.BacklogListView.Visibility = Visibility.Visible;
            }
            else
            {
                backlog.NoData.Visibility = Visibility.Visible;
                backlog.BacklogListView.Visibility = Visibility.Collapsed;
            }
        }


        /// <summary>
        /// 移动窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DragMove(object sender, PointerRoutedEventArgs e)
        {
            WindowDragHelper.DragMove(this);
        }

        /// <summary>
        /// 点击关闭按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MemuClick(object sender, RoutedEventArgs e)
        {
            try
            {
            if (!(sender is ListBox listBox) || !(listBox.SelectedItem is ListBoxItem smi))
                return;
            switch (smi.Tag?.ToString())
            {
                case "History":
                    UFG.Visibility = Visibility.Collapsed;
                    //排序历史待办 倒序

                    List<ToDoInfo> list = appData.HiToDoList.OrderByDescending(v=>v.DoneTime).ToList();
                    backlog.BacklogListView.ItemsSource = list;
                    if (backlog.BacklogListView.Items.Count > 0)
                    {
                        backlog.NoData.Visibility = Visibility.Collapsed;
                        backlog.BacklogListView.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        backlog.NoData.Visibility = Visibility.Visible;
                        backlog.BacklogListView.Visibility = Visibility.Collapsed;
                    }
                    backlog.type = ToDoType.HISTORY;
                    backlog.IsNewToDo.Text = "N";
                    UFG.Visibility = Visibility.Visible;
                    break;
                default:
                    UFG.Visibility = Visibility.Collapsed;
                    backlog.BacklogListView.ItemsSource = appData.ToDoList;
                    if (backlog.BacklogListView.Items.Count > 0)
                    {
                        backlog.NoData.Visibility = Visibility.Collapsed;
                        backlog.BacklogListView.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        backlog.NoData.Visibility = Visibility.Visible;
                        backlog.BacklogListView.Visibility = Visibility.Collapsed;
                    }
                    backlog.type = ToDoType.NEW;
                    backlog.IsNewToDo.Text = "Y";
                    UFG.Visibility = Visibility.Visible;
                    break;
            }
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "待办窗口操作失败"); }
        }

        /// <summary>
        /// 新建待办
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreateBacklog_BtnClick(object sender, RoutedEventArgs e)
        {
            ToDoInfoWindow.ShowNone();
        }


        private static Window window = null;
        public static void Show()
        {
            CloseLegacyWindow();
            MainWindow.mainWindow?.ShowAppWithTodoPanel();
        }

        public static void ShowOrHide()
        {
            CloseLegacyWindow();
            MainWindow.mainWindow?.ShowAppWithTodoPanel();
        }

        private static void CloseLegacyWindow()
        {
            if (window == null) return;

            window.Close();
            window = null;
        }


        public void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Escape)
            {
                this.DataContext = null;
                this.Close();
            }
        }
    }
}
