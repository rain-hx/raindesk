using raindesk.Constant;
using raindesk.Util;
using System.IO;

namespace raindesk.Services;

/// <summary>
/// G1: 首次启动迁移 — 将旧 APP_DIR 下的数据文件复制到 %LOCALAPPDATA%\raindesk\。
/// 仅复制不删除旧文件，用户可手动回滚。
/// </summary>
internal static class DataDirectoryMigration
{
    /// <summary>
    /// 确保数据已迁移到新目录。幂等：多次调用安全。
    /// 必须在 AppServices.ConfigureServices() 之前调用。
    /// </summary>
    public static void EnsureMigrated()
    {
        try
        {
            // 1. 确保新目录结构存在
            EnsureDirectoryExists(Constants.USER_DATA_DIR);
            EnsureDirectoryExists(Constants.DATA_FILE_BAK_DIR_PATH);
            EnsureDirectoryExists(Constants.DATA_FILE_TEMP_DIR_PATH);
            EnsureDirectoryExists(Path.Combine(Constants.USER_DATA_DIR, "logs"));
            EnsureDirectoryExists(Path.Combine(Constants.USER_DATA_DIR, "plugins"));

            // 2. 检查新位置是否已有数据文件
            if (File.Exists(Constants.DATA_FILE_PATH))
            {
                LogUtil.WriteLog("DataDirectoryMigration: 数据已在新目录，跳过迁移");
                return;
            }

            // 3. 检查旧位置是否有数据文件需要迁移
            string oldDataPath = Path.Combine(Constants.APP_DIR, "Data");
            if (File.Exists(oldDataPath))
            {
                LogUtil.WriteLog("DataDirectoryMigration: 检测到旧数据，开始迁移");
                CopyFileIfExists(oldDataPath, Constants.DATA_FILE_PATH);
                CopyDirectoryIfExists(
                    Path.Combine(Constants.APP_DIR, "bak"),
                    Constants.DATA_FILE_BAK_DIR_PATH);
                CopyDirectoryIfExists(
                    Path.Combine(Constants.APP_DIR, "temp"),
                    Constants.DATA_FILE_TEMP_DIR_PATH);
                CopyDirectoryIfExists(
                    Path.Combine(Constants.APP_DIR, "plugins"),
                    Path.Combine(Constants.USER_DATA_DIR, "plugins"));
                LogUtil.WriteLog("DataDirectoryMigration: 迁移完成");
            }
            else
            {
                LogUtil.WriteLog("DataDirectoryMigration: 新安装，无旧数据需要迁移");
            }
        }
        catch (Exception ex)
        {
            // 迁移失败不应阻止应用启动（DataService.LoadOrCreate 会处理空数据）
            LogUtil.WriteErrorLog(ex, "DataDirectoryMigration: 迁移异常，继续启动");
        }
    }

    private static void CopyFileIfExists(string source, string dest)
    {
        if (!File.Exists(source)) return;
        try
        {
            File.Copy(source, dest, overwrite: false);
            LogUtil.WriteLog($"DataDirectoryMigration: 复制 {Path.GetFileName(source)}");
        }
        catch (IOException)
        {
            // 目标已存在，跳过
        }
    }

    private static void CopyDirectoryIfExists(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        try
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in new DirectoryInfo(sourceDir).GetFiles())
            {
                string destFile = Path.Combine(destDir, file.Name);
                try
                {
                    file.CopyTo(destFile, overwrite: false);
                }
                catch (IOException)
                {
                    // 目标已存在，跳过
                }
            }
            LogUtil.WriteLog($"DataDirectoryMigration: 复制目录 {Path.GetFileName(sourceDir)}");
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, $"DataDirectoryMigration: 复制目录 {sourceDir} 失败");
        }
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
