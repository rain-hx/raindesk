using raindesk.Interface;
using raindesk.Util;
using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;

namespace raindesk.Control.Windows
{
    /// <summary>
    /// GlobalColorPickerWindow.xaml 的交互逻辑
    /// </summary>
    public partial class GlobalColorPickerWindow : Window, IWindowCommon
    {
        PixelColorPickerWindow colorPickerWindow = null;

        class PrivateDataContext : INotifyPropertyChanged
        {
            private bool copyAnimation = false;

            public bool CopyAnimation
            {
                set
                {
                    copyAnimation = value;
                    OnPropertyChanged("CopyAnimation");

                }
                get { return copyAnimation; }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public GlobalColorPickerWindow()
        {
            this.Topmost = true;
            InitializeComponent();
            raindesk.Services.ThemeService.RegisterWindow(this);
            // WinUI 3: Disable resize and hide from taskbar
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);
            this.DataContext = new PrivateDataContext();
        }

        public void OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == global::Windows.System.VirtualKey.Escape)
            {
                this.DataContext = null;
                this.Close();
            }
        }


        /// <summary>
        /// 取消按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyColorPicker_Canceled(object sender, RoutedEventArgs e)
        {
            MyColorPickerClose();
        }
        private void MyColorPicker_Confirmed(object sender, RoutedEventArgs e)
        {
            CopySuccess.Visibility = Visibility.Visible;
            PrivateDataContext pdc = this.DataContext as PrivateDataContext;
            pdc.CopyAnimation = true;
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(400);
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    pdc.CopyAnimation = false;
                    CopySuccess.Visibility = Visibility.Collapsed;
                });
            });
            string hexColor = ColorHexInput.Text;
            // WinUI 3: Use Windows.ApplicationModel.DataTransfer.Clipboard
            var dataPackage = new DataPackage();
            dataPackage.SetText(hexColor);
            Clipboard.SetContent(dataPackage);
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


        private void MyColorPicker_Checked(object sender, RoutedEventArgs e)
        {
            WindowDragHelper.HideWindow(this);
            colorPickerWindow = new PixelColorPickerWindow(color =>
            {
                ColorHexInput.Text = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
                this.Activate();
            });
            colorPickerWindow.Closed += (s, args) =>
            {
                this.Activate();
            };
            colorPickerWindow.Activate();
        }

        private void MyColorPickerClose()
        {
            this.Close();
        }

        private static Window window = null;

        public static void CreateNoShow()
        {
            if (window != null)
            {
                window.Close();
            }
            window = new GlobalColorPickerWindow();
            WindowDragHelper.HideWindow(window);
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(200);
                window.DispatcherQueue.TryEnqueue(() =>
                {
                    GlobalColorPickerWindow thisWindow = (GlobalColorPickerWindow)window;
                    thisWindow.colorPickerWindow = new PixelColorPickerWindow(color =>
                    {
                        thisWindow.ColorHexInput.Text = string.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
                        thisWindow.Activate();
                    });
                    thisWindow.colorPickerWindow.Closed += (s, args) =>
                    {
                        thisWindow.Activate();
                    };
                    thisWindow.colorPickerWindow.Activate();
                });
            });
        }

        public static void Show()
        {
            if (window != null)
            {
                window.Close();
            }
            window = new GlobalColorPickerWindow();
            window.Activate();
        }

        public static void ShowOrHide()
        {
            if (window == null)
            {
                window = new GlobalColorPickerWindow();
                window.Closed += (s, e) => { window = null; };
                window.Activate();
            }
            else
            {
                window.Close();
                window = null;
            }
        }

        private void Button_MouseEnter(object sender, PointerRoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.Opacity = 1;
        }

        private void Button_MouseLeave(object sender, PointerRoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.Opacity = 0.6;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
