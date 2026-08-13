using rainspace.Constant;
using rainspace.Features.ImportExport.Infrastructure;
using rainspace.Services.Migrations;
using rainspace.Util;
using rainspace.ViewModel;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace rainspace.Services
{
    /// <summary>
    /// 数据加载和备份恢复服务。
    /// 从 MainWindow 提取的纯数据逻辑，无 UI 依赖。
    /// </summary>
    internal static class DataService
    {
        /// <summary>
        /// 加载应用数据：优先从主文件加载，失败时尝试从备份恢复。
        /// </summary>
        public static AppData LoadOrCreate()
        {
            bool dataFileExist = File.Exists(Constants.DATA_FILE_PATH);
            AppData appData;

            if (dataFileExist)
            {
                appData = CommonCode.GetAppDataByFile(Constants.DATA_FILE_PATH);
                if (IsPlaceholderData(appData))
                {
                    appData = TryLoadFromRecoveryCandidates() ?? appData;
                }
            }
            else
            {
                appData = TryLoadFromRecoveryCandidates() ?? new AppData();
            }

            AppDataMigrationResult migration = AppDataMigrationRunner.Default.MigrateToCurrent(appData);
            if (!migration.Success)
            {
                throw new InvalidDataException(migration.Error ?? "应用数据迁移失败。");
            }

            appData = migration.Data;

            bool launcherReferencesNormalized =
                AllGroupsShortcutViewService.NormalizeOwnedFolderReferences(appData);

            if (appData.MenuList.Count == 0)
            {
                appData.MenuList.Add(new MenuInfo()
                {
                    MenuName = "NewMenu",
                    MenuId = Guid.NewGuid().ToString(),
                    MenuEdit = Visibility.Collapsed
                });
            }

            NormalizeSelectedMenu(appData);
            ResetMenuEditState(appData);
            if (ShortcutFolderService.NormalizeAllSparseFolders(appData.MenuList))
            {
                CommonCode.SaveAppDataDebounced(appData, Constants.DATA_FILE_PATH);
            }

            if (ShortcutIdentityService.EnsureIconIds(appData))
            {
                CommonCode.SaveAppDataDebounced(appData, Constants.DATA_FILE_PATH);
            }

            if (launcherReferencesNormalized)
            {
                CommonCode.SaveAppDataDebounced(appData, Constants.DATA_FILE_PATH);
            }

            if (migration.Changed)
            {
                if (!CommonCode.SaveAppData(appData, Constants.DATA_FILE_PATH))
                {
                    LogUtil.WriteLog("数据迁移已在内存完成，但写回数据文件失败；下次启动将重新迁移。");
                }
                else
                {
                    LogUtil.WriteLog(
                        $"数据迁移完成: v{migration.OriginalVersion} -> v{migration.FinalVersion}");
                }
            }

            return appData;
        }

        /// <summary>
        /// 尝试从备份文件恢复数据（启动时自动恢复）
        /// </summary>
        public static AppData? TryLoadFromBackup()
        {
            return TryLoadFromRecoveryCandidates();
        }

        private static AppData? TryLoadFromRecoveryCandidates()
        {
            try
            {
                foreach (var candidate in EnumerateRecoveryCandidates())
                {
                    AppData? data = BackupDocumentReader.Default.Read(candidate.FullName);
                    if (data == null)
                    {
                        continue;
                    }
                    AppDataMigrationResult migration = AppDataMigrationRunner.Default.MigrateToCurrent(data);
                    if (!migration.Success)
                    {
                        LogUtil.WriteLog(
                            $"跳过不兼容的恢复文件 {candidate.FullName}: {migration.Error}");
                        continue;
                    }

                    data = migration.Data;
                    if (HasShortcutData(data))
                    {
                        NormalizeSelectedMenu(data);
                        ShortcutIdentityService.EnsureIconIds(data);
                        LogUtil.WriteLog($"从有效数据恢复: {candidate.FullName}");
                        CommonCode.SaveAppData(data, Constants.DATA_FILE_PATH);
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "从有效数据恢复失败");
            }
            return null;
        }

        private static IEnumerable<FileInfo> EnumerateRecoveryCandidates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in EnumerateLocalBackups())
            {
                if (seen.Add(file.FullName)) yield return file;
            }

            foreach (var file in EnumerateDocumentRecoveryFiles())
            {
                if (seen.Add(file.FullName)) yield return file;
            }

            foreach (var file in EnumerateLegacyDataFiles())
            {
                if (seen.Add(file.FullName)) yield return file;
            }
        }

        /// <summary>
        /// 扫描用户文档目录中的迁移/导出备份（如 Data-GD-*.json）。
        /// </summary>
        private static IEnumerable<FileInfo> EnumerateDocumentRecoveryFiles()
        {
            var files = new List<FileInfo>();
            try
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (!string.IsNullOrEmpty(docs) && Directory.Exists(docs))
                {
                    var dir = new DirectoryInfo(docs);
                    files.AddRange(dir.GetFiles("Data-GD-*.json"));
                    files.AddRange(dir.GetFiles("Data-*.json"));
                    files.AddRange(dir.GetFiles("Data-GD-*.bak"));
                }
            }
            catch (Exception ex)
            {
                // 记录扫描错误但不中断恢复流程
                LogUtil.WriteLog("Documents 目录扫描异常: " + ex.Message);
            }

            return files.OrderByDescending(f => f.LastWriteTime);
        }

        private static IEnumerable<FileInfo> EnumerateLocalBackups()
        {
            string bakDir = Constants.DATA_FILE_BAK_DIR_PATH;
            if (!Directory.Exists(bakDir)) yield break;

            foreach (var file in new DirectoryInfo(bakDir)
                         .GetFiles()
                         .Where(f => f.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                                  || f.Extension.Equals(".bak", StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(f => f.LastWriteTime))
            {
                yield return file;
            }
        }

        private static IEnumerable<FileInfo> EnumerateLegacyDataFiles()
        {
            string currentDataPath = Path.GetFullPath(Constants.DATA_FILE_PATH);
            var candidates = new List<FileInfo>();

            for (DirectoryInfo? dir = new DirectoryInfo(Constants.APP_DIR); dir != null; dir = dir.Parent)
            {
                var parent = dir.Parent;
                if (parent == null) continue;

                AddIfExists(candidates, Path.Combine(parent.FullName, "rainspace-master", "bin", "Debug", "net8.0-windows", "Data"));
                AddIfExists(candidates, Path.Combine(parent.FullName, "rainspace", "bin", "Debug", "net8.0-windows", "Data"));
            }

            foreach (var file in candidates
                         .Where(f => !Path.GetFullPath(f.FullName).Equals(currentDataPath, StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(f => f.LastWriteTime))
            {
                yield return file;
            }
        }

        private static void AddIfExists(List<FileInfo> files, string path)
        {
            if (File.Exists(path)) files.Add(new FileInfo(path));
        }

        public static void ResetMenuEditState(AppData? data)
        {
            if (data?.MenuList == null) return;

            foreach (var menu in data.MenuList)
            {
                menu.MenuEdit = Visibility.Collapsed;
            }
        }

        public static void NormalizeSelectedMenu(AppData? data)
        {
            if (data?.AppConfig == null || data.MenuList == null || data.MenuList.Count == 0) return;

            // G4: 首次从旧数据启动时，从 SelectedMenuIndex 派生 SelectedGroupId
            if (string.IsNullOrEmpty(data.AppConfig.SelectedGroupId)
                && data.AppConfig.SelectedMenuIndex >= 0
                && data.AppConfig.SelectedMenuIndex < data.MenuList.Count)
            {
                data.AppConfig.SelectedGroupId = data.MenuList[data.AppConfig.SelectedMenuIndex].MenuId;
            }

            int index = data.AppConfig.SelectedMenuIndex;
            if (index < 0 || index >= data.MenuList.Count || data.MenuList[index].IconList.Count == 0)
            {
                int iconMenuIndex = -1;
                for (int i = 0; i < data.MenuList.Count; i++)
                {
                    if (data.MenuList[i].IconList.Count > 0)
                    {
                        iconMenuIndex = i;
                        break;
                    }
                }

                index = iconMenuIndex >= 0 ? iconMenuIndex : 0;
                // R6 已知例外：服务层数据规范化，在 UI 初始化前执行，无法使用命令
                data.AppConfig.SelectedMenuIndex = index;
                data.AppConfig.SelectedGroupId = data.MenuList[index].MenuId;
            }

            // R6 已知例外：服务层数据规范化，在 UI 初始化前执行，无法使用命令
            data.AppConfig.SelectedMenuIcons = data.MenuList[index].IconList;
        }

        /// <summary>
        /// 判断数据是否为占位符（默认空数据）
        /// </summary>
        public static bool IsPlaceholderData(AppData? data)
        {
            if (data?.MenuList == null) return true;
            if (data.MenuList.Count == 0) return true;

            int iconCount = CountShortcuts(data);
            if (iconCount > 0) return false;

            return data.MenuList.Count == 1
                && string.Equals(data.MenuList[0].MenuName, "NewMenu", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断数据是否包含有效的快捷方式
        /// </summary>
        public static bool HasShortcutData(AppData? data)
        {
            return data?.MenuList != null
                && data.MenuList.Count > 0
                && CountShortcuts(data) > 0;
        }

        /// <summary>
        /// 统计所有菜单中的快捷方式总数
        /// </summary>
        public static int CountShortcuts(AppData data)
        {
            int count = 0;
            foreach (var menu in data.MenuList)
            {
                if (menu?.IconList != null) count += menu.IconList.Count;
            }
            return count;
        }
    }
}
