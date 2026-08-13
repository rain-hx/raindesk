using rainspace.Constant;
using rainspace.Services;
using rainspace.Util;
using rainspace.ViewModel;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace rainspace.Task
{
    internal class BakTask
    {
        private static System.Timers.Timer? _timer;

        public static void Start()
        {
            _timer = new System.Timers.Timer
            {
                Enabled = true,
                Interval = 60 * 1000 * 60 * 2, // 2小时触发一次
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

        private static void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(Constants.DATA_FILE_BAK_DIR_PATH);

                string todayBakName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";

                int bakDays = 7;
                string? bakDaysStr = ConfigurationManager.AppSettings["BakDays"];
                if (!string.IsNullOrWhiteSpace(bakDaysStr) && !int.TryParse(bakDaysStr.Trim(), out bakDays))
                {
                    bakDays = 7;
                    LogUtil.WriteLog("BakDays 配置无效，使用默认值 7");
                }

                string bakFilePath = Path.Combine(Constants.DATA_FILE_BAK_DIR_PATH, todayBakName);
                if (dirInfo.Exists)
                {
                    FileInfo[] files = dirInfo.GetFiles()
                        .Where(f => f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                                 || f.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    if (files.Length > 0)
                    {
                        FileInfo[] sortedFiles = files.OrderByDescending(file => file.CreationTime).ToArray();
                        if (!sortedFiles[0].Name.Equals(todayBakName))
                        {
                            CreateBakFile(bakFilePath);
                        }

                        if (sortedFiles.Length > bakDays)
                        {
                            for (int i = bakDays; i < sortedFiles.Length; i++)
                            {
                                try { sortedFiles[i].Delete(); } catch { }
                            }
                        }
                    }
                    else
                    {
                        CreateBakFile(bakFilePath);
                    }
                }
                else
                {
                    dirInfo.Create();
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "备份任务异常");
            }
        }

        private static void CreateBakFile(string filePath)
        {
            try
            {
                var dataProvider = AppServices.IsConfigured
                    ? AppServices.TryGet<Services.Interfaces.IAppDataProvider>()
                    : null;
                if (dataProvider == null)
                {
                    LogUtil.WriteLog("备份跳过: 应用数据服务尚未初始化");
                    return;
                }

                if (dataProvider.Read(DataService.IsPlaceholderData))
                {
                    LogUtil.WriteLog("备份跳过: 当前数据为默认空菜单，避免覆盖有效备份");
                    return;
                }

                // 使用快照进行序列化，避免与 UI 线程的并发修改冲突
                var snapshot = dataProvider.GetSnapshot();
                Util.CommonCode.AtomicWriteAllText(filePath, snapshot.Json);
                LogUtil.WriteLog("备份创建成功: " + filePath);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "创建备份文件失败: " + filePath);
            }
        }
    }
}
