using rainspace.Constant;
using rainspace.Interface;
using rainspace.Util;
using rainspace.ViewModel;
using rainspace.Controls;
using Quartz;
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using rainspace.Services;
using rainspace.Services.Interfaces;

namespace rainspace.Control.Windows
{
    /// <summary>
    /// BacklogInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ToDoInfoWindow : Window, IWindowCommon
    {

        private static int windowType = -1;
        private static readonly int NEW_TODO = 1;
        private static readonly int DETAIL_TODO = 2;

        private readonly IToDoService _todoService = AppServices.Get<IToDoService>();

        private ToDoInfoWindow()
        {
            InitializeComponent();
            rainspace.Services.ThemeService.RegisterWindow(this);

            this.Topmost = true;
            // WinUI 3: Center on screen and disable resize
            WindowDragHelper.CenterOnScreen(this);
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);
            DateTime time = DateTime.Now.AddMinutes(10);
            // WinUI 3: Set DatePicker date programmatically
            ExeTime.Date = time;
            SetTimePanel.Visibility = Visibility.Visible;
            this.DataContext = new ToDoInfo
            {
                ExeTime = time.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        private ToDoInfoWindow(ToDoInfo info)
        {
            InitializeComponent();
            rainspace.Services.ThemeService.RegisterWindow(this);
            this.Topmost = true;
            // WinUI 3: Center on screen and disable resize
            WindowDragHelper.CenterOnScreen(this);
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);
            this.DataContext = info;
            SetTimePanel.Visibility = Visibility.Visible;
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
        /// 保存待办
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            DateTime dt;
            string execTime;
            TodoTaskExecType execType;
            if (TitleBox.Text.Trim() == "")
            {
                GrowlService.Warning("任务标题不能为空!");
                return;
            }
            else
            {
                if (SetTimePanel.Visibility == Visibility.Visible)
                {
                    execType = TodoTaskExecType.SET_TIME;
                    // WinUI 3: Get date from DatePicker
                    try
                    {
                        var selectedDate = ExeTime.Date;
                        dt = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day,
                            DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                    }
                    catch (Exception)
                    {
                        GrowlService.Warning("请输入正确的时间!");
                        return;
                    }
                    execTime = dt.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    execType = TodoTaskExecType.CRON;
                    if (Cron.Text.Trim() == "")
                    {
                        GrowlService.Warning("Cron表达式不能为空!");
                        return;
                    }
                    try
                    {
                        bool isValid = CronExpression.IsValidExpression(Cron.Text);
                        if (!isValid) throw new ArgumentException("Invalid cron expression.");
                    }
                    catch (Exception)
                    {
                        GrowlService.Warning("请输入正确的Cron表达式!");
                        return;
                    }
                    CronExpression exp = new CronExpression(Cron.Text);
                    DateTime dd = DateTime.Now;
                    DateTimeOffset ddo = DateTime.SpecifyKind(dd, DateTimeKind.Local);
                    ddo = (DateTimeOffset)exp.GetNextValidTimeAfter(ddo)!;
                    execTime = ddo.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            dt = Convert.ToDateTime(execTime);
            ToDoInfo info = new ToDoInfo
            {
                Title = TitleBox.Text,
                Msg = Msg.Text,
                ExeTime = execTime,
                ExecType = execType,
                Cron = Cron.Text
            };
            if (windowType != NEW_TODO)
            {
                ToDoInfo? existing = this.DataContext as ToDoInfo;
                if (existing == null || string.IsNullOrWhiteSpace(existing.Id))
                {
                    GrowlService.Warning("无法定位待办任务，请刷新后重试。");
                    return;
                }

                info.Id = existing.Id;
                if (_todoService.ReplaceLegacy(existing.Id, info) == null)
                {
                    GrowlService.Warning("待办任务已不存在，请刷新后重试。");
                    return;
                }
            }
            else
            {
                info = _todoService.AddLegacy(info);
            }

            DateTime dtNow = DateTime.Now;
            TimeSpan ts = dt.Subtract(dtNow);
            int minutes = (int)Math.Ceiling(ts.TotalMinutes);
            if (minutes < 0)
            {
                minutes = 0;
            }
            if (minutes > 60)
            {
                int m = minutes % 60;
                int h = minutes / 60;
                GrowlService.SuccessGlobal("设置待办任务成功, 系统将在 " + h + " 小时零 " + m + " 分钟后提醒您!");

            }
            else
            {
                GrowlService.SuccessGlobal("设置待办任务成功, 系统将在 " + minutes + " 分钟后提醒您!");
            }

            this.Close();
        }

        private static Window window = null;
        public static void ShowNone()
        {
            if (window != null)
            {
                window.Close();
            }
            window = new ToDoInfoWindow();
            window.Closed += (s, e) => { window = null; };
            window.Activate();
            windowType = NEW_TODO;
        }

        public static void ShowOrHide()
        {
            if (window == null)
            {
                window = new ToDoInfoWindow();
                window.Closed += (s, e) => { window = null; };
                window.Activate();
            }
            else
            {
                window.Close();
                window = null;
            }
        }


        public static Window GetThis()
        {
            if (window != null)
            {
                window.Close();
            }
            window = new ToDoInfoWindow();
            window.Closed += (s, e) => { window = null; };
            windowType = NEW_TODO;
            return window;
        }

        private static Window window2 = null;
        public static void ShowDetail(ToDoInfo info)
        {
            if (window2 != null)
            {
                window2.Close();
            }
            window2 = new ToDoInfoWindow(info);
            window2.Closed += (s, e) => { window2 = null; };
            windowType = DETAIL_TODO;
            window2.Activate();
        }

        private void ExecType_Checked(object sender, RoutedEventArgs e)
        {
            TodoTaskExecType tag = (TodoTaskExecType)Convert.ToInt32((sender as RadioButton).Tag.ToString());
            switch (tag)
            {
                case TodoTaskExecType.SET_TIME:
                    SetTimePanel.Visibility = Visibility.Visible;
                    CronPanel.Visibility = Visibility.Collapsed;
                    break;
                default:
                    CronPanel.Visibility = Visibility.Visible;
                    SetTimePanel.Visibility = Visibility.Collapsed;
                    break;
            }
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
