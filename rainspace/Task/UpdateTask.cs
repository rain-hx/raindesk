using rainspace.Constant;
using rainspace.Control.Windows;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.Util;
using rainspace.ViewModel;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;

namespace rainspace.Task
{
    internal class UpdateTask
    {
        // 每次读取 Store 当前状态，避免导入/恢复后继续使用旧 AppConfig 引用。
        private static AppConfig? AppCfg => AppServices.IsConfigured
            ? AppServices.TryGet<IAppDataProvider>()?.Current.AppConfig
            : null;

        private static System.Timers.Timer? _timer;

        public static void Start()
        {
            _timer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 60 * 1000 * 60 * 12, // 12小时触发一次
                AutoReset = true,
            };
            _timer.Elapsed += Timer_Elapsed;
            _timer.Start();
        }

        public static void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
        }

        // 更新地址常量（仓库无 .config 文件，ConfigurationManager.AppSettings 会返回 null）
        private const string DefaultGiteeUpdateUrl = "https://gitee.com/api/v5/repos/rymcu/rainspace/releases/latest";
        private const string DefaultGitHubUpdateUrl = "https://api.github.com/repos/rymcu/rainspace/releases/latest";
        private const string DefaultVersion = "1.0.01";

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string? updateUrl = ConfigurationManager.AppSettings["GiteeUpdateUrl"]
                    ?? DefaultGiteeUpdateUrl;
                string? nowVersion = ConfigurationManager.AppSettings["Version"]
                    ?? DefaultVersion;

                if (AppCfg != null)
                {
                    switch (AppCfg.UpdateType)
                    {
                        case UpdateType.GitHub:
                            updateUrl = ConfigurationManager.AppSettings["GitHubUpdateUrl"]
                                ?? DefaultGitHubUpdateUrl;
                            break;
                        default:
                            break;
                    }
                }

                string updateInfo = HttpUtil.Get(updateUrl);
                if (StringUtil.IsEmpty(updateInfo)) return;

                JObject jo = JObject.Parse(updateInfo);

                // 用户统计 - 使用持久化 UUID
                try
                {
                    if (jo["statisticUrl"] != null)
                    {
                        string statisticUrl = jo["statisticUrl"].ToString();
                        if (!string.IsNullOrEmpty(statisticUrl))
                        {
                            string uuid = AppCfg?.TrackingUuid ?? CommonCode.GetUniqueUUID();
                            statisticUrl += "?uuid=" + uuid;
                            HttpUtil.Get(statisticUrl);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogUtil.WriteLog("统计请求失败: " + ex.Message);
                }

                // 版本比较 - 使用 Version.Parse 而非字符串比较
                var versionToken = jo["version"];
                if (versionToken == null)
                {
                    LogUtil.WriteLog("更新检查返回的 JSON 缺少 version 字段");
                    return;
                }

                string onlineVersion = versionToken.ToString();
                if (Version.TryParse(onlineVersion, out var online)
                    && Version.TryParse(nowVersion, out var current)
                    && online > current)
                {
                    MainWindow.mainWindow?.DispatcherQueue?.TryEnqueue(() =>
                    {
                        try
                        {
                            UpdateWindow.Show(jo);
                        }
                        catch (Exception ex)
                        {
                            LogUtil.WriteErrorLog(ex, "显示更新窗口失败");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "获取更新失败!");
            }
        }
    }
}
