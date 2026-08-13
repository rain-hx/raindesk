using raindesk.Interface;
using raindesk.Util;
using raindesk.ViewModel;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Features.Launcher.Contracts;
using raindesk.Features.Launcher.Presentation;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using static raindesk.Util.ShowWindowFollowMouse;

namespace raindesk.Control.Windows
{
    /// <summary>
    /// SystemItemWindow.xaml 的交互逻辑
    /// 添加系统项目到对应菜单
    /// </summary>
    public partial class SystemItemWindow : Window, IWindowCommon
    {
        private static SystemItemViewModel vm;
        private List<IconInfo> systemIcons;
        private List<IconInfo> startMenuIcons;
        private List<IconInfo> storeIcons;
        private readonly ILauncherSystemCandidateSource _candidateSource;
        private CancellationTokenSource? _candidateLoadCancellation;

        private SystemItemWindow()
        {
            vm = new SystemItemViewModel(AppServices.Get<IAppDataProvider>().Current.AppConfig);
            _candidateSource = AppServices.Get<ILauncherSystemCandidateSource>();
            this.DataContext = vm;
            InitializeComponent();
            this.Closed += SystemItemWindow_Closed;
            raindesk.Services.ThemeService.RegisterWindow(this);
            this.Topmost = true;
            this.AppWindow.Resize(new global::Windows.Graphics.SizeInt32(1080, 680));
            // WinUI 3: Disable resize and hide from taskbar
            WindowDragHelper.DisableResize(this);
            WindowDragHelper.HideFromTaskbar(this);

            _ = LoadSystemCandidatesAsync();
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

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.DataContext = null;
            this.Close();
        }

        private void SystemItemWindow_Closed(object sender, WindowEventArgs args)
        {
            _candidateLoadCancellation?.Cancel();
            _candidateLoadCancellation?.Dispose();
            _candidateLoadCancellation = null;
            _loadingCancel?.Cancel();
            _loadingCancel?.Dispose();
            _loadingCancel = null;
            _searchTimer?.Dispose();
            _searchTimer = null;
        }

        /// <summary>
        /// 切换选项卡
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                TabViewItem ti = this.MyTabControl.SelectedItem as TabViewItem;
                if (ti == null || ti.Tag == null) return;

                switch (ti.Tag.ToString())
                {
                    case "StartMenu": //开始菜单
                        if (startMenuIcons == null)
                        {
                            vm.IconInfos = null;
                            _ = LoadStartMenuCandidatesAsync();
                        }
                        else
                        {
                            StartMenuLoading.Visibility = Visibility.Collapsed;
                            vm.IconInfos = ToObservable(startMenuIcons);
                        }
                        break;
                    case "Store": //应用商店
                        if (storeIcons == null)
                        {
                            vm.IconInfos = null;
                            storeIcons = new List<IconInfo>();
                            vm.IconInfos = ToObservable(storeIcons);
                        }
                        else
                        {
                            vm.IconInfos = ToObservable(storeIcons);
                        }
                        break;
                    default: //默认系统项
                        if (systemIcons == null)
                        {
                            vm.IconInfos = null;
                            _ = LoadSystemCandidatesAsync();
                        }
                        else
                        {
                            vm.IconInfos = ToObservable(systemIcons);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "切换选项卡失败");
            }
        }

        private async global::System.Threading.Tasks.Task LoadSystemCandidatesAsync()
        {
            CancellationToken token = ReplaceCandidateLoadCancellation();
            try
            {
                IReadOnlyList<LauncherSystemCandidate> candidates =
                    await _candidateSource.GetBuiltInSystemItemsAsync(token);
                token.ThrowIfCancellationRequested();
                systemIcons = candidates
                    .Select(LauncherSystemCandidatePresentationMapper.ToIconInfo)
                    .ToList();
                vm.IconInfos = ToObservable(systemIcons);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取系统候选项失败");
                systemIcons = new List<IconInfo>();
                vm.IconInfos = ToObservable(systemIcons);
            }
        }

        private async global::System.Threading.Tasks.Task LoadStartMenuCandidatesAsync()
        {
            CancellationToken token = ReplaceCandidateLoadCancellation();
            StartMenuLoading.Visibility = Visibility.Visible;
            try
            {
                IReadOnlyList<LauncherSystemCandidate> candidates =
                    await _candidateSource.GetStartMenuItemsAsync(token);
                token.ThrowIfCancellationRequested();
                startMenuIcons = candidates
                    .Select(LauncherSystemCandidatePresentationMapper.ToIconInfo)
                    .ToList();
                LoadListIncremental(startMenuIcons);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取开始菜单候选项失败");
                startMenuIcons = new List<IconInfo>();
                vm.IconInfos = ToObservable(startMenuIcons);
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    StartMenuLoading.Visibility = Visibility.Collapsed;
                }
            }
        }

        private CancellationToken ReplaceCandidateLoadCancellation()
        {
            _candidateLoadCancellation?.Cancel();
            _candidateLoadCancellation?.Dispose();
            _candidateLoadCancellation = new CancellationTokenSource();
            return _candidateLoadCancellation.Token;
        }

        /// <summary>
        /// 分批加载列表，避免一次性渲染卡顿
        /// </summary>
        private BulkObservableCollection<IconInfo> _sharedCollection;
        private void LoadListIncremental(List<IconInfo> source, string filterKeyword = null)
        {
            _loadingCancel?.Cancel();
            _loadingCancel = new CancellationTokenSource();
            var token = _loadingCancel.Token;

            string keyword = filterKeyword?.ToLower();

            // 复用集合，避免重新绑定
            if (_sharedCollection == null)
            {
                _sharedCollection = new BulkObservableCollection<IconInfo>();
                vm.IconInfos = _sharedCollection;
            }
            else
            {
                DispatcherQueue.TryEnqueue(() => _sharedCollection.Clear());
            }

            global::System.Threading.Tasks.Task.Run(async () =>
            {
                int batchSize = 100;
                for (int i = 0; i < source.Count; i += batchSize)
                {
                    if (token.IsCancellationRequested) return;

                    int count = Math.Min(batchSize, source.Count - i);
                    var batch = source.GetRange(i, count);

                    // 如果有搜索关键词，过滤这批数据
                    List<IconInfo> toAdd;
                    if (!string.IsNullOrEmpty(keyword))
                    {
                        toAdd = new List<IconInfo>();
                        foreach (var item in batch)
                        {
                            if (item.Name_NoWrite != null && item.Name_NoWrite.ToLower().Contains(keyword))
                            {
                                toAdd.Add(item);
                            }
                        }
                    }
                    else
                    {
                        toAdd = batch;
                    }

                    if (toAdd.Count > 0)
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            try
                            {
                                if (token.IsCancellationRequested) return;
                                _sharedCollection?.AddRange(toAdd);
                            }
                            catch (Exception ex) { LogUtil.WriteErrorLog(ex, "添加系统图标失败"); }
                        });
                    }
                    await System.Threading.Tasks.Task.Delay(30);
                }
            }, token);
        }

        private CancellationTokenSource _loadingCancel;

        private static ObservableCollection<IconInfo> ToObservable(List<IconInfo> list)
        {
            return list == null ? new ObservableCollection<IconInfo>()
                : new ObservableCollection<IconInfo>(list);
        }


        /// <summary>
        /// 搜索开始菜单应用（防抖：停止输入200ms后才执行搜索）
        /// </summary>
        private Timer _searchTimer;
        private string _lastKeyword = "";
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (startMenuIcons == null || startMenuIcons.Count == 0) return;

            // 在UI线程读取搜索文本
            string keyword = SearchBox.Text?.Trim().ToLower() ?? "";

            _searchTimer?.Dispose();
            _searchTimer = new Timer(_ =>
            {
                try
                {
                    // 避免重复搜索
                    if (keyword == _lastKeyword) return;
                    _lastKeyword = keyword;

                    if (string.IsNullOrEmpty(keyword))
                    {
                        // 清空搜索：重新渐进加载全部
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            LoadListIncremental(startMenuIcons, null);
                        });
                    }
                    else
                    {
                        // 有搜索词：重新渐进加载，边加载边过滤
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            LoadListIncremental(startMenuIcons, keyword);
                        });
                    }
                }
                catch { }
            }, null, 200, Timeout.Infinite);
        }

        public class SystemItemViewModel : INotifyPropertyChanged
        {
            private ObservableCollection<IconInfo> iconInfos;
            private AppConfig appConfig;

            public SystemItemViewModel(AppConfig appConfig)
            {
                this.AppConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            }

            public AppConfig AppConfig
            {
                get
                {
                    return appConfig;
                }
                set
                {
                    appConfig = value;
                    OnPropertyChanged("AppConfig");
                }
            }
            public ObservableCollection<IconInfo> IconInfos
            {
                get
                {
                    return iconInfos;
                }
                set
                {
                    iconInfos = value;
                    OnPropertyChanged("IconInfos");
                }
            }


            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }




        private static Window window = null;
        public static void Show()
        {
            if (window != null)
            {
                window.Close();
            }
            window = new SystemItemWindow();
            window.Closed += (s, e) => { window = null; };
            window.Activate();
            ShowWindowFollowMouse.Show(window, MousePosition.LEFT_CENTER, 0, 0);
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
