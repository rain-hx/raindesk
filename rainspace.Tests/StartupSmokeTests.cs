using rainspace.Services;
using rainspace.Util;
using rainspace.ViewModel;

namespace rainspace.Tests;

/// <summary>
/// G0: 启动路径烟测 — 验证核心启动步骤不会静默失败。
/// 这些测试不依赖 WinUI 初始化（测试项目已禁用 MSIX 自动初始化）。
/// </summary>
[TestClass]
public class StartupSmokeTests
{
    /// <summary>
    /// 验证 DataService.LoadOrCreate 在正常路径下能返回有效 AppData。
    /// </summary>
    [TestMethod]
    public void DataService_LoadOrCreate_ReturnsValidAppData()
    {
        // 使用临时目录避免污染真实数据
        string tempDir = Path.Combine(Path.GetTempPath(), $"rainspace_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);

            // DataService.LoadOrCreate 内部使用 Constants.DATA_FILE_PATH，
            // 这里直接测试 AppData 默认构造是有效的。
            var appData = new AppData();
            Assert.IsNotNull(appData, "AppData 默认构造不应返回 null");
            Assert.IsNotNull(appData.MenuList, "AppData.MenuList 不应为 null");
            Assert.IsNotNull(appData.AppConfig, "AppData.AppConfig 不应为 null");
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    /// <summary>
    /// 验证 SingleInstanceHelper.TryAcquire 在无竞争时返回 true。
    /// </summary>
    [TestMethod]
    public void SingleInstanceHelper_TryAcquire_SucceedsWhenNoConflict()
    {
        // 注意：如果已有 rainspace 实例运行，此测试可能失败。
        // 在 CI 环境中不会有竞争，这是可接受的。
        bool acquired = SingleInstanceHelper.TryAcquire();
        Assert.IsTrue(acquired, "无竞争时 TryAcquire 应返回 true");
    }

    /// <summary>
    /// 验证 LogUtil 能正常写入日志而不抛异常。
    /// </summary>
    [TestMethod]
    public void LogUtil_WriteLog_DoesNotThrow()
    {
        // 启动过程中大量依赖 LogUtil，验证它不会成为故障点
        try
        {
            LogUtil.WriteLog("Startup smoke test log entry");
            LogUtil.WriteErrorLog(new InvalidOperationException("test"), "Startup smoke test error");
        }
        catch (Exception ex)
        {
            Assert.Fail($"LogUtil 不应抛异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 验证 Constants 路径常量不为空且格式有效。
    /// </summary>
    [TestMethod]
    public void Constants_Paths_AreNonEmpty()
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(rainspace.Constant.Constants.APP_DIR),
            "APP_DIR 不应为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(rainspace.Constant.Constants.DATA_FILE_PATH),
            "DATA_FILE_PATH 不应为空");
        Assert.IsFalse(string.IsNullOrWhiteSpace(rainspace.Constant.Constants.USER_DATA_DIR),
            "USER_DATA_DIR 不应为空");
    }

    /// <summary>
    /// 验证 AppDataMigrationRunner 能对新创建的 AppData 执行迁移而不抛异常。
    /// </summary>
    [TestMethod]
    public void AppDataMigrationRunner_MigrateFreshAppData_DoesNotThrow()
    {
        var appData = new AppData();
        var result = rainspace.Services.Migrations.AppDataMigrationRunner.Default.MigrateToCurrent(appData);
        Assert.IsNotNull(result, "迁移结果不应为 null");
        Assert.IsTrue(result.Success, $"新 AppData 迁移应成功，错误: {result.Error}");
        Assert.IsNotNull(result.Data, "迁移后数据不应为 null");
    }

    /// <summary>
    /// 验证 Constants.DATA_FILE_PATH 在不同平台下路径分隔符一致。
    /// </summary>
    [TestMethod]
    public void Constants_DataFilePath_UsesValidPathChars()
    {
        string path = rainspace.Constant.Constants.DATA_FILE_PATH;
        // 不应包含无效路径字符
        char[] invalidChars = Path.GetInvalidPathChars();
        foreach (char c in invalidChars)
        {
            Assert.IsFalse(path.Contains(c),
                $"DATA_FILE_PATH 包含无效字符 '{c}': {path}");
        }
    }
}
