using rainspace.Control.UserControls.Config;
using rainspace.Interface;
using rainspace.Util;
using rainspace.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace rainspace.Control.Windows
{
    /// <summary>
    /// ConfigDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window, IWindowCommon
    {
        private readonly ThemeControl theme;
        private readonly MotionControl motion;
        private readonly OtherControl other;
        public MainWindow mainWindow;

        private ConfigWindow(AppConfig appConfig, MainWindow mainWindow)
        {
            InitializeComponent();
            rainspace.Services.ThemeService.RegisterWindow(this);
            theme = new ThemeControl();
            motion = new MotionControl();
            other = new OtherControl();
            this.DataContext = appConfig;
            RightCard.Child = theme;
            this.AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1080, 680));
            // WinUI 3: Owner window is set differently via AppWindow/WindowNative
            this.mainWindow = mainWindow;
            // WinUI 3: Disable resize and center on screen
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.CenterOnScreen(this);
            UFG.Visibility = Visibility.Visible;
        }

        private void MemuClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // sender 是 ListBox，需要获取选中的 ListBoxItem
                if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem smi)
                {
                    switch (smi.Tag?.ToString())
                    {
                        case "Motion":
                            UFG.Visibility = Visibility.Collapsed;
                            RightCard.Child = motion;
                            UFG.Visibility = Visibility.Visible;
                            break;
                        case "Theme":
                            UFG.Visibility = Visibility.Collapsed;
                            RightCard.Child = theme;
                            UFG.Visibility = Visibility.Visible;
                            break;
                        case "Other":
                            UFG.Visibility = Visibility.Collapsed;
                            RightCard.Child = other;
                            UFG.Visibility = Visibility.Visible;
                            break;
                        default:
                            UFG.Visibility = Visibility.Collapsed;
                            RightCard.Child = theme;
                            UFG.Visibility = Visibility.Visible;
                            break;
                    }
                }
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "配置窗口操作失败"); }
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

        private static ConfigWindow window = null;
        public static void Show(AppConfig appConfig, MainWindow mainWindow)
        {
            if (window != null)
            {
                window.Activate();
                return;
            }

            window = new ConfigWindow(appConfig, mainWindow);
            window.Closed += (s, e) => { window = null; };
            window.Activate();
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
