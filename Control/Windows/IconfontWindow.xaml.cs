using raindesk;
using raindesk.Constant;
using raindesk.Controls;
using raindesk.Interface;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;
using static raindesk.Util.ShowWindowFollowMouse;

namespace raindesk.Control.Windows
{
    public partial class IconfontWindow : Window, IWindowCommon
    {
        private readonly MenuInfo _menuInfo;
        private readonly List<IconfontInfo> _systemIcons;
        private readonly ISaveService _saveService;
        public static IconfontViewModel vm;

        private bool _isWindowDragging;
        private global::Windows.Foundation.Point _dragScreenStart;
        private global::Windows.Graphics.PointInt32 _windowStartPos;

        private IconfontWindow(List<IconfontInfo> icons, MenuInfo menuInfo)
        {
            _saveService = AppServices.Get<ISaveService>();
            InitializeComponent();
            SystemIcon.IconListBox.SelectionChanged += IconSelectionChanged;
            ConfigurePopupWindow();
            raindesk.Services.ThemeService.RegisterWindow(this);

            _systemIcons = icons;
            this.Topmost = true;
            _menuInfo = menuInfo;
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);
            vm = new IconfontViewModel
            {
                Iconfonts = _systemIcons
            };
            this.DataContext = vm;
            Activated += IconfontWindow_Activated;
            HookWindowDragHandlers();
            RootGrid.Loaded += (_, _) =>
            {
                EnsureWindowLayoutReady();
                SelectCurrentIcon();
            };
        }

        private void EnsureWindowLayoutReady()
        {
            ApplyScaledWindowSize();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                ApplyScaledWindowSize();
                RootGrid.UpdateLayout();
            });
        }

        private void HookWindowDragHandlers()
        {
            TitleBarHost.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnRootPointerPressed), true);
            TitleBarHost.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler(OnRootPointerMoved), true);
            TitleBarHost.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(OnRootPointerReleased), true);
            TitleBarHost.AddHandler(UIElement.PointerCanceledEvent, new PointerEventHandler(OnRootPointerCanceled), true);
        }

        private void IconfontWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == WindowActivationState.Deactivated) return;
            EnsureWindowLayoutReady();
        }

        private double GetRasterizationScale() => RootGrid.XamlRoot?.RasterizationScale ?? 1.0d;

        private void ApplyScaledWindowSize()
        {
            if (RootGrid.XamlRoot == null) return;

            const double logicalWidth = 700;
            const double logicalHeight = 520;
            double scale = GetRasterizationScale();
            AppWindow.Resize(new global::Windows.Graphics.SizeInt32(
                (int)Math.Ceiling(logicalWidth * scale),
                (int)Math.Ceiling(logicalHeight * scale)));
        }

        private void OnRootPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            bool canDrag = CanStartWindowDrag(e.OriginalSource as DependencyObject);

            if (!canDrag || !e.GetCurrentPoint(RootGrid).Properties.IsLeftButtonPressed) return;

            _isWindowDragging = true;
            _dragScreenStart = e.GetCurrentPoint(null).Position;
            _windowStartPos = AppWindow.Position;
            TitleBarHost.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void OnRootPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isWindowDragging) return;

            var pos = e.GetCurrentPoint(null).Position;
            double scale = GetRasterizationScale();
            int dx = (int)Math.Round((pos.X - _dragScreenStart.X) * scale);
            int dy = (int)Math.Round((pos.Y - _dragScreenStart.Y) * scale);
            var newPos = new global::Windows.Graphics.PointInt32(
                _windowStartPos.X + dx,
                _windowStartPos.Y + dy);
            AppWindow.Move(newPos);
            e.Handled = true;
        }

        private void OnRootPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            EndWindowDrag(e);
        }

        private void OnRootPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            EndWindowDrag(e);
        }

        private bool CanStartWindowDrag(DependencyObject? source)
        {
            for (DependencyObject? current = source; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current == ButtonPanel) return false;
                if (current is Button or ScrollBar or Thumb) return false;
            }

            return true;
        }

        private void EndWindowDrag(PointerRoutedEventArgs e)
        {
            if (!_isWindowDragging) return;

            _isWindowDragging = false;
            TitleBarHost.ReleasePointerCapture(e.Pointer);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = null;
            this.Close();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            int index = this.SystemIcon.IconListBox.SelectedIndex;
            if (index < 0 || index >= _systemIcons.Count)
            {
                return;
            }

            _menuInfo.MenuGeometry = _systemIcons[index].Text;
            MainWindow.mainWindow?.LeftCard?.RefreshMenuIcon(_menuInfo);
            MainWindow.mainWindow?.RightCard?.RefreshGroupIcon(_menuInfo);
            _saveService.SaveDebounced();
            this.DataContext = null;
            this.Close();
        }

        private void IconSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfirmButton.IsEnabled = SystemIcon.IconListBox.SelectedIndex >= 0;
        }

        private void SelectCurrentIcon()
        {
            int index = _systemIcons.FindIndex(icon => icon.Text == _menuInfo.MenuGeometry);
            SystemIcon.IconListBox.SelectedIndex = index;
            ConfirmButton.IsEnabled = index >= 0;
        }

        private static IconfontWindow? window;
        public static void Show(List<IconfontInfo> listInfo, MenuInfo menuInfo)
        {
            if (window != null)
            {
                window.Close();
            }

            var newWindow = new IconfontWindow(listInfo, menuInfo);
            window = newWindow;
            MainWindow.mainWindow?.BeginDialogSuppressLostFocusHide();
            newWindow.Closed += (_, _) =>
            {
                MainWindow.mainWindow?.EndDialogSuppressLostFocusHide();
                if (ReferenceEquals(window, newWindow))
                {
                    window = null;
                }
            };
            newWindow.Activate();
            ShowWindowFollowMouse.Show(newWindow, MousePosition.LEFT_CENTER, 0, 0);
        }

        private void ConfigurePopupWindow()
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsMinimizable = false;
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

        public class IconfontViewModel : INotifyPropertyChanged
        {
            private List<IconfontInfo> iconfonts;

            public List<IconfontInfo> Iconfonts
            {
                get => iconfonts;
                set
                {
                    iconfonts = value;
                    OnPropertyChanged("Iconfonts");
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
