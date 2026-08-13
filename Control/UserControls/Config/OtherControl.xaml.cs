using raindesk.Constant;
using raindesk.Control.Windows;
using raindesk.Controls;
using raindesk.Features.ImportExport.Contracts;
using raindesk.MyThread;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Plugins.EveryThing;
using raindesk.Util;
using raindesk.ViewModel;
using Newtonsoft.Json.Linq;
using ShowSeconds;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace raindesk.Control.UserControls.Config
{
    /// <summary>
    /// OtherControl.xaml 的交互逻辑
    /// </summary>
    public partial class OtherControl : UserControl
    {
        private readonly AppConfig appConfig;
        private readonly ISaveService saveService;
        private bool isLoaded;

        public OtherControl()
        {
            appConfig = AppServices.Get<IAppDataProvider>().Current.AppConfig;
            saveService = AppServices.Get<ISaveService>();
            this.InitializeComponent();
            this.DataContext = appConfig;
            this.Loaded += OtherControl_Loaded;

        }

        private void OtherControl_Loaded(object sender, RoutedEventArgs e)
        {
            appConfig.SelfStartUp = RegisterUtil.IsAutoStart(Constants.MY_NAME);
            Sort_Check();
            isLoaded = true;
        }

        private void SelfStartUpBox_Click(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            RegisterUtil.SetSelfStarting(appConfig.SelfStartUp, Constants.MY_NAME);
            appConfig.SelfStartUp = RegisterUtil.IsAutoStart(Constants.MY_NAME);
        }

        private void ShowBarIcon_Changed(object sender, RoutedEventArgs e)
        {
            if (!isLoaded) return;

            MainWindow.mainWindow?.RefreshTrayIcon();
            saveService.SaveDebounced();
        }

        private void SortType_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            SortType type = (SortType)int.Parse(rb.Tag.ToString());

            SortType resType = type;
            switch (type)
            {
                case SortType.CUSTOM:
                    break;
                case SortType.COUNT_UP:
                    if (rb.IsChecked == true)
                    {
                        CountLowSort.IsChecked = true;
                        CountUpSort.Visibility = Visibility.Collapsed;
                        CountLowSort.Visibility = Visibility.Visible;
                        resType = SortType.COUNT_LOW;
                    }
                    break;
                case SortType.COUNT_LOW:
                    if (rb.IsChecked == true)
                    {
                        CountUpSort.IsChecked = true;
                        CountLowSort.Visibility = Visibility.Collapsed;
                        CountUpSort.Visibility = Visibility.Visible;
                        resType = SortType.COUNT_UP;
                    }
                    break;
                case SortType.NAME_UP:
                    if (rb.IsChecked == true)
                    {
                        NameLowSort.IsChecked = true;
                        NameUpSort.Visibility = Visibility.Collapsed;
                        NameLowSort.Visibility = Visibility.Visible;
                        resType = SortType.NAME_LOW;
                    }
                    break;
                case SortType.NAME_LOW:
                    if (rb.IsChecked == true)
                    {
                        NameUpSort.IsChecked = true;
                        NameLowSort.Visibility = Visibility.Collapsed;
                        NameUpSort.Visibility = Visibility.Visible;
                        resType = SortType.NAME_UP;
                    }
                    break;
            }
            appConfig.IconSortType = resType;
            CommonCode.SortIconList();
        }

        private void Sort_Check()
        {
            if (NameLowSort.IsChecked == true)
            {
                NameUpSort.Visibility = Visibility.Collapsed;
                NameLowSort.Visibility = Visibility.Visible;
            }
            if (CountLowSort.IsChecked == true)
            {
                CountUpSort.Visibility = Visibility.Collapsed;
                CountLowSort.Visibility = Visibility.Visible;
            }
        }

        private async void BakDataFile(object sender, RoutedEventArgs e)
        {
            IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
            IBackupService? backupService = AppServices.TryGet<IBackupService>();
            if (picker == null || backupService == null)
            {
                await ShowInfoDialog("备份失败", "备份服务尚未初始化。");
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
                await ShowInfoDialog("备份成功", "数据已导出到 .bak 文件。");
            }
            else
            {
                await ShowInfoDialog("备份失败", result.Error ?? "无法写入备份文件。");
            }
        }

        private async void ImportBackupFile(object sender, RoutedEventArgs e)
        {
            IBackupFilePicker? picker = AppServices.TryGet<IBackupFilePicker>();
            IBackupService? backupService = AppServices.TryGet<IBackupService>();
            if (picker == null || backupService == null)
            {
                await ShowInfoDialog("导入失败", "备份服务尚未初始化。");
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
                await ShowInfoDialog("导入失败", result.Error ?? "无法导入备份文件。");
                return;
            }

            await ShowInfoDialog("导入成功", "备份导入成功！应用将重新启动以加载新数据。");
            ProcessUtil.ReStartApp();
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            try
            {
                string updateUrl = GetSelectedUpdateUrl();
                if (string.IsNullOrWhiteSpace(updateUrl))
                {
                    await ShowInfoDialog("检查更新", "当前更新源未配置更新地址。");
                    return;
                }

                string updateInfo = await System.Threading.Tasks.Task.Run(() => HttpUtil.Get(updateUrl));
                if (StringUtil.IsEmpty(updateInfo))
                {
                    await ShowInfoDialog("检查更新失败", "无法获取更新信息，请稍后再试。");
                    return;
                }

                JObject jo = JObject.Parse(updateInfo);
                string onlineVersion = jo["version"]?.ToString();
                string currentVersion = GetCurrentVersion();
                if (string.IsNullOrWhiteSpace(onlineVersion))
                {
                    await ShowInfoDialog("检查更新失败", "更新信息中缺少版本号。");
                    return;
                }

                if (IsNewerVersion(onlineVersion, currentVersion))
                {
                    UpdateWindow.Show(jo);
                    return;
                }

                await ShowInfoDialog("已是最新版本", "当前版本 " + currentVersion + "，更新源版本 " + onlineVersion + "。");
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "手动检查更新失败");
                await ShowInfoDialog("检查更新失败", "更新信息解析失败，请稍后再试。");
            }
            finally
            {
                CheckUpdateButton.IsEnabled = true;
            }
        }

        private string GetSelectedUpdateUrl()
        {
            string key = appConfig.UpdateType == UpdateType.GitHub ? "GitHubUpdateUrl" : "GiteeUpdateUrl";
            return ConfigurationManager.AppSettings[key];
        }

        private static string GetCurrentVersion()
        {
            string version = ConfigurationManager.AppSettings["Version"];
            if (!string.IsNullOrWhiteSpace(version)) return version;

            try
            {
                var packageVersion = global::Windows.ApplicationModel.Package.Current.Id.Version;
                return packageVersion.Major + "." + packageVersion.Minor + "." + packageVersion.Build + "." + packageVersion.Revision;
            }
            catch
            {
                return typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            }
        }

        private static bool IsNewerVersion(string onlineVersion, string currentVersion)
        {
            if (Version.TryParse(onlineVersion, out Version? online) && Version.TryParse(currentVersion, out Version? current))
            {
                return online > current;
            }

            return string.Compare(onlineVersion, currentVersion, StringComparison.OrdinalIgnoreCase) > 0;
        }

        private async System.Threading.Tasks.Task ShowInfoDialog(string title, string content)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = content,
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot ?? MainWindow.mainWindow?.Content?.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch
            {
                // Fallback if ContentDialog fails
                System.Diagnostics.Debug.WriteLine($"{title}: {content}");
            }
        }

        private void ShowSeconds_Click(object sender, RoutedEventArgs e)
        {
            if (appConfig.SecondsWindow == true)
            {
                SecondsWindow.ShowWindow();
            }
            else
            {
                SecondsWindow.CloseWindow();
            }
        }

        public static void StopSecondsWindow()
        {
            if (MessageUtil.CheckWindowIsRuning("ShowSeconds_Main_" + Constants.MY_UUID))
            {
                MessageUtil.SendMsgByWName(
                    "ShowSeconds_Main_" + Constants.MY_UUID,
                    "Shutdown"
                    );
            }
        }

        public static void StartSecondsWindow()
        {
            try
            {
                using (var objOS = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject objMgmt in objOS.Get())
                    {
                        if (objMgmt.Properties["Caption"].Value != null)
                        {
                            string caption = objMgmt.Properties["Caption"].Value.ToString(); ;
                            LogUtil.WriteLog("获取的系统版本号为:" + caption);
                            if (caption.Contains("Windows 11"))
                            {
                                //找到ShowSeconds插件
                                FileInfo fi = FileUtil.GetFileByNameWithDir("ShowSeconds.exe", Constants.PLUGINS_PATH);
                                if (fi == null)
                                {
                                    GrowlService.WarningGlobal("未安装程序插件: ShowSeconds");
                                }
                                else
                                {
                                    //检查是否在运行
                                    if (!MessageUtil.CheckWindowIsRuning("ShowSeconds_Main_" + Constants.MY_UUID))
                                    {
                                        using (Process p = new Process())
                                        {
                                            p.StartInfo.FileName = fi.FullName;
                                            p.StartInfo.WorkingDirectory = fi.FullName.Substring(0, fi.FullName.LastIndexOf("\\"));
                                            p.Start();
                                        }
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "启动ShowSeconds插件失败");
            }
        }

        /// <summary>
        /// EveryThing插件开关
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EveryThing_Changed(object sender, RoutedEventArgs e)
        {
            if (appConfig.EnableEveryThing == true)
            {
                EveryThingUtil.EnableEveryThing(0);
            } else
            {
                EveryThingUtil.DisableEveryThing(true);
            }
        }
    }
}
