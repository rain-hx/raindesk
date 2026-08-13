using raindesk.Constant;
using raindesk.Control.UserControls.Backlog;
using raindesk.Control.UserControls.PannelCard;
using raindesk.Control.Windows;
using raindesk.Controls;
using raindesk.Features.ImportExport.Contracts;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Features.Tray.Contracts;
using raindesk.Util;
using raindesk.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using static raindesk.Services.NativeWindowMethods;

namespace raindesk
{
    public sealed partial class MainWindow : Window
    {
        private AppData appData => _dataProvider.Current;
        public AppData AppDataInstance => appData;
        public static MainWindow mainWindow;

        internal StackPanel? ToastContainerHost => ToastContainer;

        // DataContext shim for WinUI 3 Window
        private object _dataContext;
        public object DataContext
        {
            get => _dataContext;
            set
            {
                _dataContext = value;
                if (Content is FrameworkElement fe) fe.DataContext = value;
            }
        }

        // WinUI 3 Window doesn't have Topmost/Visibility like WPF
        public bool Topmost { get => false; set { } }
        public Visibility Visibility { get => Visibility.Visible; set { } }

        /// <summary>拖拽预览覆盖层 Canvas</summary>
        internal Canvas DragOverlayCanvasHost => DragOverlayCanvas;

        internal void SetIconDragActive(bool active)
        {
            _isIconDragActive = active;
            if (active)
            {
                CancelScheduledLostFocusHide();
            }
        }

        // 类型安全的属性替代 dynamic，避免运行时绑定错误
        public object PDDialog { get; set; }
        public Microsoft.UI.Xaml.Controls.Grid WrapUFG { get; set; }
        public Microsoft.UI.Xaml.Controls.ListView IconListBox { get; set; }
        public object MenuListBox => LeftCard?.MenuListBox;
        private ITrayController? _trayService;
        private readonly IAppDataProvider _dataProvider;
        private readonly IHotkeyRegistrar _hotkeyRegistrar;
        private WindowGeometryService? _geometryService;
        private NavigationService? _navigationService;
        private bool _allowClose;
        private bool _isRedirectingMinimize;
        private IntPtr _windowSubclassHwnd;
        private IntPtr _originalWndProc;
        private WindowProcDelegate? _windowProcDelegate;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _foregroundWatchTimer;
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _lostFocusHideTimer;
        private bool _isTopMostActive;
        private bool _isTopMostOpening;
        private bool _isInWindowMoveSizeLoop;
        private bool _isIconDragActive;
        private int _dialogSuppressLostFocusHideCount;
        private int _topMostSessionId;
        private UserActivityHook? _topMostMouseHook;
        private IDisposable? _themeRegistration;
        private AppConfig? _observedAppConfig;
        private bool _searchBoxFocused;
        private bool _searchBoxHovered;

        public MainWindow()
        {
            try
            {
                LogUtil.WriteLog("MainWindow ctor start");

                _dataProvider = AppServices.Get<IAppDataProvider>();
                _hotkeyRegistrar = AppServices.Get<IHotkeyRegistrar>();

                // 使用 DataService 加载数据，并显式挂入唯一 Store。
                AppData loadedData = DataService.LoadOrCreate();
                _dataProvider.Replace(loadedData);

                // 兼容旧界面的直接模型绑定：集中观察已挂入 Store 的持久化属性。
                Services.AppServices.TryGet<Services.AppDataChangeTracker>()?.Start();

                ImageUtil.PreloadImages(appData.MenuList);
                _hotkeyRegistrar.NormalizeDefaults();
                mainWindow = this;

                // 初始化窗口服务
                _trayService = Services.AppServices.TryGet<ITrayController>();
                _geometryService = Services.AppServices.TryGet<Services.WindowGeometryService>();
                _navigationService = Services.AppServices.TryGet<Services.NavigationService>();

                this.InitializeComponent();
                _dataProvider.StateReplaced += AppDataProvider_StateReplaced;
                DataContext = appData;

                ThemeService.SetMode(appData.AppConfig.AppThemeMode);
                _themeRegistration = ThemeService.RegisterWindow(this, ApplyThemeChrome);
                ShowMainPanel();

                // 恢复窗口位置和大小
                if (_geometryService != null)
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    _geometryService.Restore(this.AppWindow, hwnd);
                    this.AppWindow.Changed += _geometryService.CreateChangedHandler(this.AppWindow);
                }
                else
                {
                    RestoreWindowSize();
                }

                this.Title = "raindesk";
                ConfigureWindowChrome();
                WindowRoot.Loaded += WindowRoot_Loaded;
                WindowRoot.SizeChanged += (_, _) =>
                {
                    UpdateTitleBarRegions();
                    UpdateSearchResultsDropDownPosition();
                    RightCard?.ScheduleShortcutLayoutUpdateFromWindow();
                };
                TopBarHost.SizeChanged += (s, e) => UpdateTitleBarRegions();
                SearchBoxHost.SizeChanged += (s, e) =>
                {
                    UpdateTitleBarRegions();
                    UpdateSearchResultsDropDownPosition();
                };
                PageCloseButton.SizeChanged += (s, e) => UpdateTitleBarRegions();
                TopSettingsButton.SizeChanged += (s, e) => UpdateTitleBarRegions();
                BGSettingUtil.BGSetting();
                ObserveAppConfig(appData.AppConfig);
                ApplyGlassBackdrop();
                InstallWindowMessageHook();
                this.AppWindow.Closing += (s, e) =>
                {
                    if (!_allowClose && appData?.AppConfig?.ShowBarIcon == true)
                    {
                        e.Cancel = true;
                        HideApp();
                        LogUtil.WriteLog("MainWindow close redirected to tray");
                    }
                };

                // Set DataContext
                this.Activated += MainWindow_Activated;

                // Keyboard shortcuts
                this.Content.PreviewKeyDown += OnContentKeyDown;

                // Setup tray icon via TrayService
                SetupTrayService();

                // 使用 HotkeyService 注册热键
                HotkeyService.Initialize(
                    DispatcherQueue,
                    ShowApp,
                    ShowApp);
                appData.AppConfig.EnableTodoHotKey = false;
                _hotkeyRegistrar.RegisterAll(false);

                this.Closed += (s, e) =>
                {
                    LogUtil.WriteLog("MainWindow closed");
                    _dataProvider.StateReplaced -= AppDataProvider_StateReplaced;
                    ObserveAppConfig(null);
                    RestoreWindowMessageHook();
                    _themeRegistration?.Dispose();
                    _hotkeyRegistrar.UnregisterAll();
                    // 使用 Exchange 防止双重释放
                    Interlocked.Exchange(ref _topMostMouseHook, null)?.Dispose();
                    _trayService?.Dispose();
                };

                // Start background tasks
                try { Task.BakTask.Start(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "启动备份任务失败"); }
                try { Task.UpdateTask.Start(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "启动更新任务失败"); }

                // 检查是否需要显示首次使用引导
                _ = CheckAndShowOnboardingAsync();

                DevDiagnosticService.Start(DispatcherQueue);

                LogUtil.WriteLog("MainWindow ctor complete");
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "MainWindow error");
                throw;
            }
        }

        private void AppDataProvider_StateReplaced(AppData replacement)
        {
            if (!DispatcherQueue.HasThreadAccess)
            {
                DispatcherQueue.TryEnqueue(() => AppDataProvider_StateReplaced(_dataProvider.Current));
                return;
            }

            replacement = _dataProvider.Current;
            DataContext = replacement;
            ObserveAppConfig(replacement.AppConfig);
            ThemeService.SetMode(replacement.AppConfig.AppThemeMode);
            ApplySearchBoxVisibility();
            ApplyGlassBackdrop();
            RefreshTrayIcon();
        }

        #region Tray Icon

        internal void RefreshTrayIcon()
        {
            _trayService?.Refresh(appData?.AppConfig?.ShowBarIcon == true);
        }

        private void SetupTrayService()
        {
            if (_trayService == null) return;

            _trayService.OnShowRequested = () => ShowApp();
            _trayService.OnOpenPanel = () => ShowApp();
            _trayService.OnExportBackup = () => ExportBackupAsync();
            _trayService.OnImportBackup = () => ImportBackupAsync();
            _trayService.OnExitRequested = () => ExitApp();

            _trayService.Initialize(appData?.AppConfig?.ShowBarIcon == true);
        }

        private void ExitApp()
        {
            _allowClose = true;
            if (_geometryService != null)
            {
                _geometryService.PersistImmediate(this.AppWindow);
            }
            else
            {
                PersistWindowGeometryImmediate();
            }


            // 停止后台任务
            try { Task.BakTask.Stop(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "停止备份任务失败"); }
            try { Task.UpdateTask.Stop(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "停止更新任务失败"); }

            // 释放钩子（使用 Exchange 防止双重释放）
            try { Interlocked.Exchange(ref _topMostMouseHook, null)?.Dispose(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "释放鼠标钩子失败"); }
            try { _hotkeyRegistrar.UnregisterAll(); } catch (Exception ex) { LogUtil.WriteErrorLog(ex, "注销热键失败"); }

            // 释放托盘图标
            _trayService?.Dispose();

            Application.Current.Exit();
        }

        #endregion

        // Load Data 已移至 Services.DataService

        #region Show / Hide

        public void ShowApp()
        {
            try
            {
                if (appData?.AppConfig?.FollowMouse == true)
                {
                    ShowWindowFollowMouse.MoveToMouse(this);
                }

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                int sessionId = ++_topMostSessionId;
                _isTopMostOpening = true;
                _isTopMostActive = true;
                NativeWindowMethods.ShowWindow(hwnd, SW_RESTORE);
                NativeWindowMethods.ShowWindow(hwnd, SW_SHOW);
                ShowMainPanel();
                NativeWindowMethods.SetWindowPos(hwnd, NativeWindowMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeWindowMethods.SWP_NOMOVE | NativeWindowMethods.SWP_NOSIZE | NativeWindowMethods.SWP_SHOWWINDOW | NativeWindowMethods.SWP_NOOWNERZORDER);
                NativeWindowMethods.ActivateWindow(hwnd);
                this.Activate();
                StartForegroundWatch();
                StartTopMostMouseWatch();
                ConfirmTopMostAfterOpen(hwnd, sessionId);
                PlayShowAnimation();

                // Safety net: ensure opacity is restored even if the animation is interrupted
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    await System.Threading.Tasks.Task.Delay(350).ConfigureAwait(false);
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            if (ContentShell.Opacity < 0.5)
                            {
                                ContentShell.Opacity = 1;
                                ContentShell.RenderTransform = new CompositeTransform();
                            }
                        }
                        catch (Exception ex) { LogUtil.WriteErrorLog(ex, "恢复窗口透明度失败"); }
                    });
                });
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "ShowApp 失败"); }
        }

        private void ConfirmTopMostAfterOpen(IntPtr hwnd, int sessionId)
        {
            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await System.Threading.Tasks.Task.Delay(120).ConfigureAwait(false);
                DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        if (sessionId != _topMostSessionId) return;
                        if (hwnd == IntPtr.Zero) return;

                        ForceTopMost(hwnd);
                        ActivateWindow(hwnd);
                        _isTopMostActive = true;
                        _isTopMostOpening = false;
                        StartForegroundWatch();
                        StartTopMostMouseWatch();
                    }
                    catch
                    {
                        _isTopMostOpening = false;
                    }
                });
            });
        }

        private void WindowRoot_Loaded(object sender, RoutedEventArgs e)
        {
            WindowRoot.Loaded -= WindowRoot_Loaded;
            UpdateSearchPlaceholderVisibility();
            ApplySearchBoxVisibility();

            if (SearchBox != null && SearchInputMirror != null && SearchCaretBlinker != null)
            {
                BlinkingCaretOverlay.Attach(SearchBox, SearchInputMirror, SearchCaretBlinker, DispatcherQueue);
            }
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_dataContext == null) DataContext = appData;

            if (args.WindowActivationState == WindowActivationState.Deactivated)
            {
                if (ShouldSuppressLostFocusHide)
                {
                    CancelScheduledLostFocusHide();
                    return;
                }

                ReleaseTopMost();
                ScheduleHideOnLostFocus();
            }
            else
            {
                CancelScheduledLostFocusHide();
            }
        }

        private void StartForegroundWatch()
        {
            if (_foregroundWatchTimer == null)
            {
                _foregroundWatchTimer = DispatcherQueue.CreateTimer();
                _foregroundWatchTimer.Interval = TimeSpan.FromMilliseconds(250);
                _foregroundWatchTimer.Tick += (s, e) =>
                {
                    if (!_isTopMostActive)
                    {
                        _foregroundWatchTimer.Stop();
                        return;
                    }

                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    if (hwnd == IntPtr.Zero) return;

                    if (ShouldSuppressLostFocusHide)
                    {
                        return;
                    }

                    if (NativeWindowMethods.IsLeftMouseDownOutside(hwnd))
                    {
                        CancelTopMost(hwnd);
                        return;
                    }

                    if (NativeWindowMethods.IsForegroundWindowFor(hwnd))
                    {
                        return;
                    }
                };
            }

            _foregroundWatchTimer.Start();
        }

        private void ReleaseTopMost()
        {
            try
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hwnd == IntPtr.Zero) return;

                ReleaseTopMost(hwnd, false);
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "释放置顶状态失败"); }
        }

        private void ReleaseTopMost(IntPtr hwnd, bool force)
        {
            if (_isTopMostOpening && !force) return;

            SetWindowPos(hwnd, NativeWindowMethods.HWND_NOTOPMOST, 0, 0, 0, 0, NativeWindowMethods.SWP_NOMOVE | NativeWindowMethods.SWP_NOSIZE | NativeWindowMethods.SWP_NOACTIVATE | NativeWindowMethods.SWP_NOOWNERZORDER);
            _isTopMostActive = false;
            _isTopMostOpening = false;
            _foregroundWatchTimer?.Stop();
            StopTopMostMouseWatch();
        }

        private void ForceTopMost(IntPtr hwnd)
        {
            SetWindowPos(hwnd, NativeWindowMethods.HWND_TOPMOST, 0, 0, 0, 0, NativeWindowMethods.SWP_NOMOVE | NativeWindowMethods.SWP_NOSIZE | NativeWindowMethods.SWP_SHOWWINDOW | NativeWindowMethods.SWP_NOOWNERZORDER);
        }

        private void StartTopMostMouseWatch()
        {
            try
            {
                StopTopMostMouseWatch();
                _topMostMouseHook = new UserActivityHook(true, false);
                _topMostMouseHook.OnMouseLeftDown += TopMostMouseHook_OnMouseLeftDown;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "置顶失焦鼠标监听启动失败");
            }
        }

        private void StopTopMostMouseWatch()
        {
            try
            {
                if (_topMostMouseHook == null) return;

                _topMostMouseHook.OnMouseLeftDown -= TopMostMouseHook_OnMouseLeftDown;
                _topMostMouseHook.Stop(true, false, false);
                _topMostMouseHook = null;
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "停止鼠标监听失败"); }
        }

        private void TopMostMouseHook_OnMouseLeftDown(object sender, int button, int clickCount, int x, int y, int delta)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hwnd == IntPtr.Zero) return;

                if (ShouldSuppressLostFocusHide || NativeWindowMethods.IsPointWithinWindowBounds(hwnd, x, y))
                {
                    return;
                }

                if (NativeWindowMethods.IsPointOutsideWindow(hwnd, x, y))
                {
                    CancelTopMost(hwnd, x, y);
                }
            });
        }

        private void CancelTopMost(IntPtr hwnd)
        {
            CancelTopMost(hwnd, null, null);
        }

        private void CancelTopMost(IntPtr hwnd, int? x, int? y)
        {
            _topMostSessionId++;
            ReleaseTopMost(hwnd, true);
            if (x.HasValue && y.HasValue)
            {
                NativeWindowMethods.BringWindowAtPointAbove(hwnd, x.Value, y.Value);
            }

            TryHideOnLostFocusImmediate();
        }

        // BringWindowAtPointAbove, IsPointOutsideWindow, ContainsPoint, IsForegroundWindowFor
        // 已移至 NativeWindowMethods

        private void PlayShowAnimation()
        {
            if (!MotionPreferences.ShouldAnimate)
            {
                ContentShell.Opacity = 1;
                return;
            }

            try
            {
                // Use From on animations instead of setting base values to 0.
                // If the animation is interrupted (e.g. window loses focus immediately),
                // the base values remain at 1.0 so the window is always visible.
                ContentShell.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
                ContentShell.RenderTransform = new CompositeTransform();

                var storyboard = new Storyboard();

                var ease = new QuinticEase { EasingMode = EasingMode.EaseOut };

                var scaleX = new DoubleAnimation
                {
                    From = 0.96,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(220),
                    EasingFunction = ease
                };
                Storyboard.SetTarget(scaleX, ContentShell.RenderTransform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");

                var scaleY = new DoubleAnimation
                {
                    From = 0.96,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(220),
                    EasingFunction = ease
                };
                Storyboard.SetTarget(scaleY, ContentShell.RenderTransform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");

                var opacity = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(160),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(opacity, ContentShell);
                Storyboard.SetTargetProperty(opacity, "Opacity");

                storyboard.Children.Add(scaleX);
                storyboard.Children.Add(scaleY);
                storyboard.Children.Add(opacity);
                storyboard.Begin();
            }
            catch
            {
                // Fallback: ensure the window is visible if animation setup fails
                ContentShell.Opacity = 1;
                ContentShell.RenderTransform = new CompositeTransform();
            }
        }

        private AppHideType CurrentHideType => appData?.AppConfig?.AppHideType ?? AppHideType.START_EXE;

        private bool IsConfiguredForLostFocusHide => CurrentHideType == AppHideType.LOST_FOCUS;

        private bool ShouldSuppressLostFocusHide =>
            _dialogSuppressLostFocusHideCount > 0
            || MainWindowMessagePolicy.ShouldSuppressLostFocusHide(
                _isInWindowMoveSizeLoop,
                _isIconDragActive);

        internal void BeginDialogSuppressLostFocusHide()
        {
            _dialogSuppressLostFocusHideCount++;
            CancelScheduledLostFocusHide();
        }

        internal void EndDialogSuppressLostFocusHide()
        {
            if (_dialogSuppressLostFocusHideCount > 0)
            {
                _dialogSuppressLostFocusHideCount--;
            }
        }

        private bool ShouldHideOnLostFocus => MainWindowMessagePolicy.ShouldHideOnLostFocus(
            IsConfiguredForLostFocusHide,
            ShouldSuppressLostFocusHide);

        private void ScheduleHideOnLostFocus()
        {
            if (!ShouldHideOnLostFocus) return;

            _lostFocusHideTimer ??= DispatcherQueue.CreateTimer();
            _lostFocusHideTimer.Interval = TimeSpan.FromMilliseconds(250);
            _lostFocusHideTimer.Stop();
            _lostFocusHideTimer.Tick -= LostFocusHideTimer_Tick;
            _lostFocusHideTimer.Tick += LostFocusHideTimer_Tick;
            _lostFocusHideTimer.Start();
        }

        private void CancelScheduledLostFocusHide()
        {
            _lostFocusHideTimer?.Stop();
        }

        private void LostFocusHideTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
        {
            sender.Stop();
            if (!ShouldHideOnLostFocus) return;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd)) return;

            if (!IsForegroundWindowFor(hwnd))
            {
                HideApp();
            }
        }

        private void TryHideOnLostFocusImmediate()
        {
            if (!ShouldHideOnLostFocus) return;

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            if (hwnd == IntPtr.Zero || !IsWindowVisible(hwnd)) return;

            HideApp();
        }

        public void HideApp()
        {
            try
            {
                CancelScheduledLostFocusHide();
                if (_geometryService != null)
                {
                    _geometryService.PersistImmediate(this.AppWindow);
                }
                else
                {
                    PersistWindowGeometryImmediate();
                }
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (hwnd != IntPtr.Zero)
                {
                    _topMostSessionId++;
                    ReleaseTopMost(hwnd, true);
                }

                NativeWindowMethods.ShowWindow(hwnd, SW_HIDE);
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "HideApp 失败"); }
        }

        public void HidedSearchBox()
        {
            CloseSearchResultsDropDown();
            if (SearchBoxHost != null)
            {
                SearchBoxHost.Visibility = Visibility.Collapsed;
            }

            UpdateTitleBarRegions();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateTitleBarRegions);
        }

        public void ShowSearchBox()
        {
            EnsureSearchBoxVisible();
            SearchBox?.Focus(FocusState.Programmatic);
        }

        public void ApplySearchBoxVisibility()
        {
            if (appData?.AppConfig?.HideSearchBox == true)
            {
                HidedSearchBox();
                return;
            }

            CloseSearchResultsDropDown();
            EnsureSearchBoxVisible();
        }

        private void EnsureSearchBoxVisible()
        {
            if (SearchBoxHost == null)
            {
                return;
            }

            SearchBoxHost.Visibility = Visibility.Visible;
            SearchBoxHost.IsHitTestVisible = true;
            if (SearchBox != null)
            {
                SearchBox.IsHitTestVisible = true;
            }

            UpdateTitleBarRegions();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, UpdateTitleBarRegions);
        }

        public static void ShowAppStatic() { mainWindow?.ShowApp(); }
        public static void HideAppStatic() { mainWindow?.HideApp(); }

        /// <summary>旧待办入口兼容：显示窗口时统一进入首页。</summary>
        public void ShowAppWithTodoPanel()
        {
            ShowApp();
            ShowMainPanel();
        }

        private void InstallWindowMessageHook()
        {
            try
            {
                if (_windowProcDelegate != null) return;

                _windowSubclassHwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (_windowSubclassHwnd == IntPtr.Zero) return;

                _windowProcDelegate = WindowMessageProc;
                IntPtr proc = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(_windowProcDelegate);
                _originalWndProc = NativeWindowMethods.SetWindowLongPtr(_windowSubclassHwnd, NativeWindowMethods.GWLP_WNDPROC, proc);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "安装主窗口消息监听失败");
            }
        }

        private void RestoreWindowMessageHook()
        {
            try
            {
                if (_windowSubclassHwnd == IntPtr.Zero || _originalWndProc == IntPtr.Zero) return;

                NativeWindowMethods.SetWindowLongPtr(_windowSubclassHwnd, NativeWindowMethods.GWLP_WNDPROC, _originalWndProc);
                _windowSubclassHwnd = IntPtr.Zero;
                _originalWndProc = IntPtr.Zero;
                _windowProcDelegate = null;
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "恢复窗口消息钩子失败"); }
        }

        private IntPtr WindowMessageProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (MainWindowMessagePolicy.IsBeginMoveSizeMessage(msg, wParam))
            {
                _isInWindowMoveSizeLoop = true;
                CancelScheduledLostFocusHide();
            }
            else if (MainWindowMessagePolicy.IsEndMoveSizeMessage(msg))
            {
                _isInWindowMoveSizeLoop = false;
                CancelScheduledLostFocusHide();
                if (_geometryService != null)
                {
                    _geometryService.PersistImmediate(this.AppWindow);
                }
                else
                {
                    PersistWindowGeometryImmediate();
                }
            }

            if (MainWindowMessagePolicy.ShouldScheduleHideOnLostFocus(msg, wParam, ShouldSuppressLostFocusHide))
            {
                ReleaseTopMost(hwnd, false);
                ScheduleHideOnLostFocus();
            }

            if (!_allowClose && !_isRedirectingMinimize && MainWindowMessagePolicy.ShouldRedirectMinimize(msg, wParam))
            {
                try
                {
                    _isRedirectingMinimize = true;
                    NativeWindowMethods.ShowWindow(hwnd, SW_HIDE);
                    LogUtil.WriteLog("MainWindow minimize redirected to tray");
                }
                finally
                {
                    _isRedirectingMinimize = false;
                }

                return IntPtr.Zero;
            }

            return NativeWindowMethods.CallWindowProc(_originalWndProc, hwnd, msg, wParam, lParam);
        }

        private delegate IntPtr WindowProcDelegate(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        // P/Invoke 声明已移至 Services.NativeWindowMethods

        #endregion

        // 热键注册已移至 Services.HotkeyService

        // 兼容性保留：供外部代码调用
        public static void RegisterHotKey(bool showMsg) => HotkeyService.RegisterMainHotKey(showMsg);
        public static void RegisterCreateToDoHotKey(bool showMsg) => HotkeyService.RegisterToDoHotKey(showMsg);
        public static void NormalizeHotKeyDefaults() => HotkeyService.NormalizeHotKeyDefaults();

        #region Event Handlers

        private void ConfigureWindowChrome()
        {
            try
            {
                ExtendsContentIntoTitleBar = true;
                this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;

                ApplyThemeChrome(ThemeService.CurrentMode);

                if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(true, false);
                    presenter.PreferredMinimumWidth = 480;
                    presenter.PreferredMinimumHeight = 600;
                }

                UpdateTitleBarRegions();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "隐藏系统标题栏失败");
            }
        }

        private void UpdateTitleBarRegions()
        {
            try
            {
                if (Content?.XamlRoot == null || TopBarHost == null) return;

                double scale = Content.XamlRoot.RasterizationScale;
                var titleBarSource = Microsoft.UI.Input.InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
                titleBarSource.SetRegionRects(
                    Microsoft.UI.Input.NonClientRegionKind.Caption,
                    new[] { ToRectInt32(GetElementBounds(TopBarHost), scale) });

                var passthroughRects = new List<Windows.Graphics.RectInt32>();
                TryAddPassthroughRegion(passthroughRects, SearchBoxHost, scale);
                TryAddPassthroughRegion(passthroughRects, TopSettingsButton, scale);
                TryAddPassthroughRegion(passthroughRects, PageCloseButton, scale);
                titleBarSource.SetRegionRects(
                    Microsoft.UI.Input.NonClientRegionKind.Passthrough,
                    passthroughRects.ToArray());
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "更新系统标题栏区域失败");
            }
        }

        private void TryAddPassthroughRegion(
            List<Windows.Graphics.RectInt32> rects,
            FrameworkElement? element,
            double scale)
        {
            if (element == null || element.Visibility != Visibility.Visible)
            {
                return;
            }

            if (ReferenceEquals(element, SearchBoxHost) && appData?.AppConfig?.HideSearchBox == true)
            {
                return;
            }

            if (element.ActualWidth <= 0.01d || element.ActualHeight <= 0.01d)
            {
                return;
            }

            global::Windows.Foundation.Rect bounds = GetElementBounds(element);
            if (bounds.Width <= 0.01d || bounds.Height <= 0.01d)
            {
                return;
            }

            rects.Add(ToRectInt32(bounds, scale));
        }

        private global::Windows.Foundation.Rect GetElementBounds(FrameworkElement element)
        {
            return element
                .TransformToVisual(BGBorder)
                .TransformBounds(new global::Windows.Foundation.Rect(0, 0, element.ActualWidth, element.ActualHeight));
        }

        private static Windows.Graphics.RectInt32 ToRectInt32(global::Windows.Foundation.Rect bounds, double scale)
        {
            return new Windows.Graphics.RectInt32(
                (int)Math.Round(bounds.X * scale),
                (int)Math.Round(bounds.Y * scale),
                Math.Max(1, (int)Math.Round(bounds.Width * scale)),
                Math.Max(1, (int)Math.Round(bounds.Height * scale)));
        }

        private bool IsHotkeyInputFocused()
        {
            if (_hotkeyRegistrar.IsCapturing)
            {
                return true;
            }

            if (FocusManager.GetFocusedElement(Content.XamlRoot) is not TextBox textBox)
            {
                return false;
            }

            string? tag = textBox.Tag?.ToString();
            return tag == "Main" || tag == "ToDo";
        }

        private async void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (IsHotkeyInputFocused())
            {
                return;
            }

            var leftCtrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftControl);
            var rightCtrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightControl);
            bool isCtrlPressed = leftCtrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)
                || rightCtrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            var leftShift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.LeftShift);
            var rightShift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.RightShift);
            bool isShiftPressed = leftShift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)
                || rightShift.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                if (InlineConfigPanel?.Visibility == Visibility.Visible)
                {
                    ShowMainPanel();
                    e.Handled = true;
                    return;
                }

                if (SearchResultsDropDown?.Visibility == Visibility.Visible)
                {
                    CloseSearchResultsDropDown();
                    e.Handled = true;
                    return;
                }

                HidedSearchBox();
                if (CurrentHideType != AppHideType.HANDLE)
                {
                    HideApp();
                }
                e.Handled = true;
            }
            else if (isCtrlPressed && e.Key == Windows.System.VirtualKey.F)
            {
                if (appData?.AppConfig?.HideSearchBox != true)
                {
                    // Ctrl+F: 聚焦搜索框
                    SearchBox?.Focus(FocusState.Keyboard);
                }

                e.Handled = true;
            }
            else if (isCtrlPressed && e.Key == Windows.System.VirtualKey.N)
            {
                // Ctrl+N: 添加新快捷方式
                await ShowAddShortcutDialogAsync();
                e.Handled = true;
            }
            else if (isCtrlPressed && e.Key == Windows.System.VirtualKey.Z && !isShiftPressed)
            {
                // Ctrl+Z: 撤销
                UndoHelper.Undo();
                e.Handled = true;
            }
            else if (isCtrlPressed && isShiftPressed && e.Key == Windows.System.VirtualKey.Z)
            {
                // Ctrl+Shift+Z: 重做
                UndoHelper.Redo();
                e.Handled = true;
            }
            else if (isCtrlPressed && e.Key == Windows.System.VirtualKey.Y)
            {
                // Ctrl+Y: 重做
                UndoHelper.Redo();
                e.Handled = true;
            }
            else if (e.Key == Windows.System.VirtualKey.F1)
            {
                // F1: 显示帮助
                _ = HelpHelper.ShowHelpAsync();
                e.Handled = true;
            }
        }

        private async System.Threading.Tasks.Task ShowAddShortcutDialogAsync()
        {
            var dialog = new ContentDialog
            {
                Title = "添加快捷方式",
                Content = "选择要添加的快捷方式类型\n\n• URL 项目：添加网页链接\n• 系统项目：添加系统应用",
                PrimaryButtonText = "URL 项目",
                SecondaryButtonText = "系统项目",
                CloseButtonText = "取消",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                RightCard?.ShowAddUrlPanel();
            }
            else if (result == ContentDialogResult.Secondary)
            {
                RightCard?.ShowAddSystemPanel();
            }
        }

        /// <summary>
        /// 检查并显示首次使用引导
        /// </summary>
        private async System.Threading.Tasks.Task CheckAndShowOnboardingAsync()
        {
            try
            {
                if (OnboardingHelper.ShouldShowOnboarding())
                {
                    // 等待窗口完全加载
                    await System.Threading.Tasks.Task.Delay(1000);
                    await OnboardingHelper.ShowOnboardingAsync();
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "CheckAndShowOnboardingAsync error");
            }
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            HideApp();
        }

        private void ConfigMenuClick(object sender, RoutedEventArgs e)
        {
            ShowInlineConfigPanel();
        }

        private void HomeRailClick(object sender, RoutedEventArgs e)
        {
            ShowMainPanel();
        }

        private void SettingsRailClick(object sender, RoutedEventArgs e)
        {
            if (InlineConfigPanel?.Visibility == Visibility.Visible)
            {
                ShowMainPanel();
            }
            else
            {
                ShowInlineConfigPanel();
            }
        }

        private void BacklogMenuClick(object sender, RoutedEventArgs e)
        {
            ShowInlineTodoPanel();
        }

        private void ShowInlineConfigPanel()
        {
            bool panelChanged = _navigationService?.ActivePanel != PanelType.Settings
                || InlineConfigPanel.Visibility != Visibility.Visible;
            ConfigPanel.DataContext = appData.AppConfig;
            LeftColumn.Width = new GridLength(0);
            if (_navigationService != null)
            {
                _navigationService.ShowSettingsPanel(
                    appData.AppConfig, ConfigPanel, InlineConfigPanel,
                    InlineTodoPanel, LeftCard, RightCard);
            }
            else
            {
                LeftCard.Visibility = Visibility.Collapsed;
                RightCard.Visibility = Visibility.Collapsed;
                InlineTodoPanel.Visibility = Visibility.Collapsed;
                InlineConfigPanel.Visibility = Visibility.Visible;
            }

            if (panelChanged)
            {
                SurfaceMotion.PlayEntrance(InlineConfigPanel, translateX: 10d, translateY: 0d);
            }
        }

        private TodoPanel? _todoPanel;

        private TodoPanel EnsureTodoPanelLoaded()
        {
            if (_todoPanel == null)
            {
                _todoPanel = new TodoPanel();
                TodoPanelHost.Content = _todoPanel;
            }

            return _todoPanel;
        }

        private void ShowInlineTodoPanel()
        {
            bool panelChanged = _navigationService?.ActivePanel != PanelType.Todo
                || InlineTodoPanel.Visibility != Visibility.Visible;
            var todoPanel = EnsureTodoPanelLoaded();
            todoPanel.Refresh();
            LeftColumn.Width = new GridLength(0);
            if (_navigationService != null)
            {
                _navigationService.ShowTodoPanel(
                    todoPanel, InlineConfigPanel, LeftCard, RightCard, InlineTodoPanel);
            }
            else
            {
                LeftCard.Visibility = Visibility.Collapsed;
                RightCard.Visibility = Visibility.Collapsed;
                InlineConfigPanel.Visibility = Visibility.Collapsed;
                InlineTodoPanel.Visibility = Visibility.Visible;
            }

            if (panelChanged)
            {
                SurfaceMotion.PlayEntrance(InlineTodoPanel, translateX: 10d, translateY: 0d);
            }
        }

        private void ShowMainPanel()
        {
            bool panelChanged = _navigationService?.ActivePanel != PanelType.Home
                || RightCard.Visibility != Visibility.Visible;
            if (_navigationService != null)
            {
                _navigationService.ShowMainPanel(
                    InlineConfigPanel, InlineTodoPanel, LeftCard, RightCard,
                    new GridLength(0));
            }
            else
            {
                if (InlineConfigPanel != null) InlineConfigPanel.Visibility = Visibility.Collapsed;
                if (InlineTodoPanel != null) InlineTodoPanel.Visibility = Visibility.Collapsed;
                LeftColumn.Width = new GridLength(0);
                LeftCard.Visibility = Visibility.Collapsed;
                RightCard.Visibility = Visibility.Visible;
            }

            if (panelChanged && WindowRoot?.IsLoaded == true)
            {
                SurfaceMotion.PlayEntrance(RightCard, translateX: -8d, translateY: 0d);
            }
        }

        private void ObserveAppConfig(AppConfig? config)
        {
            if (_observedAppConfig != null)
            {
                _observedAppConfig.PropertyChanged -= AppConfig_PropertyChanged;
            }

            _observedAppConfig = config;
            if (_observedAppConfig != null)
            {
                _observedAppConfig.PropertyChanged += AppConfig_PropertyChanged;
            }
        }

        private void AppConfig_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppConfig.BlurEffect) || e.PropertyName == nameof(AppConfig.BlurValue))
            {
                ApplyGlassBackdrop();
            }
            else if (e.PropertyName == nameof(AppConfig.HideSearchBox))
            {
                ApplySearchBoxVisibility();
            }
        }

        public void ApplyGlassBackdrop()
        {
            GlassBackdropUtil.ApplyMainWindowGlass(this);
            RightCard?.RefreshGlassBackdrop();
            ConfigPanel?.RefreshGlassBackdrop();
            _todoPanel?.RefreshGlassBackdrop();
        }

        private void ApplyThemeChrome(AppThemeMode mode)
        {
            try
            {
                var titleBarColor = ThemeService.GetTitleBarColor(mode);
                this.AppWindow.TitleBar.BackgroundColor = titleBarColor;
                this.AppWindow.TitleBar.InactiveBackgroundColor = titleBarColor;
                BGSettingUtil.BGSetting();
                ApplyGlassBackdrop();
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "应用主题窗口标题栏失败");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSearchPlaceholderVisibility();
            OnSearchBoxTextChanged(SearchBox?.Text);
        }

        private void UpdateSearchPlaceholderVisibility()
        {
            if (SearchPlaceholderText == null || SearchBox == null)
            {
                return;
            }

            SearchPlaceholderText.Visibility = string.IsNullOrEmpty(SearchBox.Text) && !_searchBoxFocused
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _searchBoxFocused = true;
            UpdateSearchPlaceholderVisibility();
            UpdateSearchBoxChrome();
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _searchBoxFocused = false;
            UpdateSearchPlaceholderVisibility();
            UpdateSearchBoxChrome();
        }

        private void SearchBoxHost_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (_searchBoxFocused)
            {
                return;
            }

            _searchBoxHovered = true;
            UpdateSearchBoxChrome();
        }

        private void SearchBoxHost_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (_searchBoxFocused)
            {
                return;
            }

            _searchBoxHovered = false;
            UpdateSearchBoxChrome();
        }

        private void UpdateSearchBoxChrome()
        {
            if (SearchBoxHost == null) return;

            var resources = Application.Current.Resources;
            var normalBorder = resources["LauncherShellBorderBrush"] as Brush;
            var hoverBorder = resources["AccentLightBrush"] as Brush;
            var focusBorder = resources["raindeskBlueBrush"] as Brush;
            var normalBackground = resources["LauncherSearchBackgroundBrush"] as Brush;
            var focusBackground = resources["SurfaceBrush"] as Brush;
            var secondaryForeground = resources["SecondaryTextBrush"] as Brush;

            if (_searchBoxFocused)
            {
                SearchBoxHost.BorderBrush = focusBorder ?? normalBorder;
                SearchBoxHost.BorderThickness = new Thickness(2);
                SearchBoxHost.Background = focusBackground ?? normalBackground;
                if (SearchBox != null)
                {
                    // The rounded host owns the fill. An opaque TextBox fill creates
                    // a square rectangle inside it and hides the host's corner radius.
                    SearchBox.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                }

                if (SearchGlyphIcon != null)
                {
                    SearchGlyphIcon.Foreground = focusBorder ?? secondaryForeground;
                }

                return;
            }

            SearchBoxHost.BorderThickness = new Thickness(1);
            SearchBoxHost.Background = normalBackground;
            if (SearchBox != null)
            {
                SearchBox.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }

            if (_searchBoxHovered)
            {
                SearchBoxHost.BorderBrush = hoverBorder ?? normalBorder;
            }
            else
            {
                SearchBoxHost.BorderBrush = normalBorder;
            }

            if (SearchGlyphIcon != null)
            {
                SearchGlyphIcon.Foreground = secondaryForeground;
            }
        }

        /// <summary>
        /// 显示全局加载状态
        /// </summary>
        public void ShowGlobalLoading(string message = "加载中...")
        {
            if (GlobalLoadingOverlay != null)
            {
                GlobalLoadingText.Text = message;
                GlobalLoadingOverlay.Visibility = Visibility.Visible;
                GlobalLoadingOverlay.IsHitTestVisible = true;
                GlobalLoadingRing.IsActive = true;
                SurfaceMotion.PlayEntrance(GlobalLoadingOverlay, translateY: 0d, duration: MotionTokens.Fast);
            }
        }

        /// <summary>
        /// 隐藏全局加载状态
        /// </summary>
        public void HideGlobalLoading()
        {
            if (GlobalLoadingOverlay != null && GlobalLoadingOverlay.Visibility == Visibility.Visible)
            {
                GlobalLoadingOverlay.IsHitTestVisible = false;
                GlobalLoadingRing.IsActive = false;
                SurfaceMotion.PlayExit(GlobalLoadingOverlay, () =>
                {
                    GlobalLoadingOverlay.Visibility = Visibility.Collapsed;
                }, translateY: 0d, duration: MotionTokens.Exit);
            }
        }

        /// <summary>
        /// 显示 Toast 通知
        /// </summary>
        public void ShowToast(string message, ToastType type = ToastType.Info)
        {
            ToastHelper.Show(message, type);
        }

        private void ImportBackupFile(object sender, RoutedEventArgs e)
        {
            _ = ImportBackupAsync();
        }

        private async System.Threading.Tasks.Task ImportBackupAsync()
        {
            // 先显示窗口，否则文件选择器无法弹出
            ShowApp();

            IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
            IBackupService? backupService = AppServices.TryGet<IBackupService>();
            if (picker == null || backupService == null)
            {
                var unavailableDialog = new ContentDialog
                {
                    Title = "导入失败",
                    Content = "备份服务尚未初始化。",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };
                await unavailableDialog.ShowAsync();
                return;
            }

            string? sourcePath = await picker.PickImportPathAsync();
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return;
            }

            BackupOperationResult result = await backupService.ImportAsync(sourcePath);
            if (!result.Success)
            {
                var failedDialog = new ContentDialog
                {
                    Title = "导入失败",
                    Content = result.Error ?? "无法导入备份文件。",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };
                await failedDialog.ShowAsync();
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "导入成功",
                Content = "备份导入成功！应用将重新启动以加载新数据。",
                CloseButtonText = "确定",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
            ProcessUtil.ReStartApp();
        }

        private async System.Threading.Tasks.Task ExportBackupAsync()
        {
            IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
            IBackupService? backupService = AppServices.TryGet<IBackupService>();
            if (picker == null || backupService == null)
            {
                return;
            }

            string? destinationPath = await picker.PickExportPathAsync(
                "Data-GD-" + DateTime.Now.ToString("yyMMdd"));
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                return;
            }

            BackupOperationResult result = await backupService.ExportAsync(destinationPath);
            if (result.Success)
            {
                var dialog = new ContentDialog
                {
                    Title = "备份成功",
                    Content = "数据已导出到 .bak 文件。",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            else
            {
                var dialog = new ContentDialog
                {
                    Title = "备份失败",
                    Content = result.Error ?? "无法写入备份文件。",
                    CloseButtonText = "确定",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        #endregion

        #region Window Size Memory

        /// <summary>
        /// 恢复窗口位置和大小
        /// </summary>
        private void RestoreWindowSize()
        {
            try
            {
                var config = appData?.AppConfig;
                if (config == null)
                {
                    this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1080, 680));
                    return;
                }

                int width = (int)(config.WindowWidth > 0 ? config.WindowWidth : 1080);
                int height = (int)(config.WindowHeight > 0 ? config.WindowHeight : 680);

                // 确保窗口大小在合理范围内
                width = Math.Max(480, Math.Min(width, 2560));
                height = Math.Max(600, Math.Min(height, 1440));

                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                // 恢复窗口位置（如果有保存的话）
                if (config.WindowLeft >= 0 && config.WindowTop >= 0)
                {
                    // 使用 MoveWindow 恢复位置
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                    if (hwnd != IntPtr.Zero)
                    {
                        NativeWindowMethods.MoveWindow(hwnd, (int)config.WindowLeft, (int)config.WindowTop, width, height, true);
                    }
                }

                // 监听窗口移动和大小变化
                this.AppWindow.Changed += AppWindow_Changed;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "恢复窗口大小失败");
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1080, 680));
            }
        }

        /// <summary>
        /// 窗口属性变化时保存位置和大小
        /// </summary>
        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            try
            {
                if (args.DidSizeChange || args.DidPositionChange)
                {
                    SaveWindowSize();
                }
            }
            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "窗口属性变化处理失败"); }
        }

        /// <summary>
        /// 保存窗口位置和大小
        /// </summary>
        private void SaveWindowSize()
        {
            try
            {
                var config = appData?.AppConfig;
                if (config == null) return;

                var size = this.AppWindow.Size;
                var position = this.AppWindow.Position;

                config.WindowWidth = size.Width;
                config.WindowHeight = size.Height;
                config.WindowLeft = position.X;
                config.WindowTop = position.Y;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "保存窗口大小失败");
            }
        }

        private void PersistWindowGeometryImmediate()
        {
            SaveWindowSize();
            if (appData == null) return;

            CommonCode.FlushDebouncedSave(appData, Constants.DATA_FILE_PATH);
        }

        #endregion
    }
}
