using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using raindesk.Constant;
using raindesk.Features.ImportExport.Infrastructure;
using raindesk.Services;
using raindesk.ViewModel;

namespace raindesk.Util
{
    public static class CommonCode
    {
        // PERF: Debounce timer to prevent excessive disk writes.
        private static Timer? _saveDebounceTimer;
        private static readonly object _saveLock = new object();
        private static long _saveGeneration;
        private const int SaveDebounceMs = 500;

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        #region Atomic File Write

        /// <summary>
        /// 原子写入：先写临时文件，再替换原文件。
        /// 即使进程在写入中途崩溃，原文件也不会损坏。
        /// 委托给 SafeFileWriter 以获得互斥锁保护。
        /// </summary>
        internal static void AtomicWriteAllText(string filePath, string content)
        {
            SafeFileWriter.WriteAllText(filePath, content);
        }

        #endregion

        #region Save

        public static bool SaveAppData(AppData appData, string filePath)
        {
            try
            {
                if (appData == null) return false;

                string json = JsonConvert.SerializeObject(appData, Formatting.Indented, _jsonSettings);
                AtomicWriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "保存数据失败");
                return false;
            }
        }

        public static async global::System.Threading.Tasks.Task SaveAppDataAsync(AppData appData, string filePath)
        {
            try
            {
                if (appData == null) return;

                string json = JsonConvert.SerializeObject(appData, Formatting.Indented, _jsonSettings);
                await global::System.Threading.Tasks.Task.Run(() => AtomicWriteAllText(filePath, json)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "保存数据失败");
            }
        }

        public static void SaveAppDataDebounced(AppData appData, string filePath)
        {
            if (appData == null) return;

            lock (_saveLock)
            {
                long generation = ++_saveGeneration;
                _saveDebounceTimer?.Dispose();
                _saveDebounceTimer = new Timer(_ =>
                {
                    try
                    {
                        SaveDebouncedGeneration(appData, filePath, generation);
                    }
                    catch (Exception ex)
                    {
                        LogUtil.WriteErrorLog(ex, "延迟保存数据失败");
                    }
                }, null, SaveDebounceMs, Timeout.Infinite);
            }
        }

        private static void SaveDebouncedGeneration(AppData appData, string filePath, long generation)
        {
            lock (_saveLock)
            {
                if (generation != _saveGeneration)
                    return;

                _saveDebounceTimer?.Dispose();
                _saveDebounceTimer = null;
                string json = JsonConvert.SerializeObject(appData, Formatting.Indented, _jsonSettings);
                AtomicWriteAllText(filePath, json);
            }
        }

        public static void CancelDebouncedSave()
        {
            lock (_saveLock)
            {
                ++_saveGeneration;
                _saveDebounceTimer?.Dispose();
                _saveDebounceTimer = null;
            }
        }

        /// <summary>
        /// 立即写入当前数据，并取消待执行的防抖保存。
        /// </summary>
        public static void FlushDebouncedSave(AppData appData, string filePath)
        {
            if (appData == null) return;

            lock (_saveLock)
            {
                ++_saveGeneration;
                _saveDebounceTimer?.Dispose();
                _saveDebounceTimer = null;
                try
                {
                    string json = JsonConvert.SerializeObject(appData, Formatting.Indented, _jsonSettings);
                    AtomicWriteAllText(filePath, json);
                }
                catch (Exception ex)
                {
                    LogUtil.WriteErrorLog(ex, "立即保存数据失败");
                }
            }
        }

        #endregion

        #region Load

        public static async global::System.Threading.Tasks.Task<AppData> GetAppDataByFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return await global::System.Threading.Tasks.Task.Run(() => LoadAppDataFromFile(filePath)).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取数据失败");
                // Auto-recovery: try to restore from backup
                var recovered = TryRecoverFromBackup();
                if (recovered != null)
                {
                    SaveAppData(recovered, filePath);
                    return recovered;
                }
            }
            return new AppData();
        }

        public static AppData GetAppDataByFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return LoadAppDataFromFile(filePath);
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "读取数据失败");
                // Auto-recovery: try to restore from backup
                var recovered = TryRecoverFromBackup();
                if (recovered != null)
                {
                    SaveAppData(recovered, filePath);
                    return recovered;
                }
            }
            return new AppData();
        }

        private static AppData LoadAppDataFromFile(string filePath)
        {
            var normalized = BackupDocumentReader.Default.Read(filePath);
            if (normalized != null)
            {
                // 仅在规范化有实际变更时才保存，避免每次启动都写入
                bool changed = ShortcutIdentityService.EnsureIconIds(normalized);
                changed |= AllGroupsShortcutViewService.NormalizeOwnedFolderReferences(normalized);
                changed |= HydrateImportedIconData(normalized);
                if (changed)
                {
                    SaveAppData(normalized, filePath);
                }
                return normalized;
            }
            return new AppData();
        }

        #endregion

        #region Auto-Recovery

        /// <summary>
        /// 当主数据文件损坏时，尝试从 bak/ 目录中自动恢复最新的备份
        /// </summary>
        private static AppData? TryRecoverFromBackup()
        {
            try
            {
                string bakDir = Constants.DATA_FILE_BAK_DIR_PATH;
                if (!Directory.Exists(bakDir)) return null;

                // 按创建时间倒序扫描 JSON 备份文件
                var bakFiles = new DirectoryInfo(bakDir).GetFiles("*.json");
                Array.Sort(bakFiles, (a, b) => b.CreationTime.CompareTo(a.CreationTime));

                foreach (var bakFile in bakFiles)
                {
                    try
                    {
                        var data = BackupDocumentReader.Default.Read(bakFile.FullName);
                        if (data != null && data.MenuList != null)
                        {
                            LogUtil.WriteLog($"自动恢复成功，使用备份文件: {bakFile.Name}");
                            return data;
                        }
                    }
                    catch
                    {
                        // This backup is also corrupt, try next
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "自动恢复备份失败");
            }
            return null;
        }

        #endregion

        #region Import / Export

        #endregion

        #region Helpers

        /// <summary>
        /// 根据路径创建 IconInfo 并加载图标
        /// 支持：普通文件、快捷方式(.lnk)、系统项(::{...})、目录
        /// </summary>
        public static IconInfo GetIconInfoByPath(string path)
        {
            string tempPath = path;
            string iconPath = path;
            string startArg = null;

            try
            {
                string ext = System.IO.Path.GetExtension(path)?.ToLower() ?? "";

                // 快捷方式：解析目标路径和图标路径
                if (".lnk".Equals(ext))
                {
                    try
                    {
                        string targetPath = FileUtil.GetTargetPathByLnk(path);
                        iconPath = FileUtil.GetIconPathByLnk(path) ?? path;
                        startArg = FileUtil.GetArgByLnk(path);
                        if (targetPath != null) tempPath = targetPath;
                    }
                    catch (Exception ex) { LogUtil.WriteErrorLog(ex, "解析快捷方式失败: " + path); }
                }

                byte[] imageBytes = ImageUtil.GetImageBytesByPath(iconPath);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    imageBytes = Convert.FromBase64String(Constants.DEFAULT_IMG_IMAGE_BASE64);
                }

                // 加载图标。优先使用已持久化的字节，避免 BitmapImage 和 byte[] 状态脱节。
                Microsoft.UI.Xaml.Media.Imaging.BitmapImage bi = ImageUtil.ByteArrToImage(imageBytes);
                if (bi == null)
                {
                    try
                    {
                        bi = ImageUtil.GetBitmapIconByPath(iconPath);
                    }
                    catch (Exception ex) { LogUtil.WriteErrorLog(ex, "获取图标失败: " + iconPath); }
                }

                var iconInfo = new IconInfo
                {
                    Path = tempPath,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    LnkPath = (".lnk".Equals(ext)) ? path : null,
                    StartArg = startArg,
                };

                // 设置图标数据
                // 重要：先设置 byte[]，再设置 BitmapImage
                // 因为 BitmapImage_NoWrite setter 会调用 BitmapImageToByte（WinUI 3 中返回 null）清空 imageByteArr
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    iconInfo.ImageByteArr_NoWrite = imageBytes;
                    iconInfo.DefaultImage_NoWrite = imageBytes;
                }
                if (bi != null)
                {
                    // 直接设置 bitmapImage 字段避免触发 BitmapImageToByte
                    iconInfo.SetBitmapImageDirect(bi);
                }

                return iconInfo;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "GetIconInfoByPath 失败: " + path);
                return new IconInfo
                {
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    ImageByteArr_NoWrite = Convert.FromBase64String(Constants.DEFAULT_IMG_IMAGE_BASE64),
                    DefaultImage_NoWrite = Convert.FromBase64String(Constants.DEFAULT_IMG_IMAGE_BASE64)
                };
            }
        }

        /// <summary>
        /// 对图标列表进行排序
        /// </summary>
        /// <param name="sort">是否排序</param>
        /// <remarks>
        /// TODO: 实现排序功能 - 需要根据 SortType 对图标列表进行排序
        /// 支持的排序类型：CUSTOM, NAME, TYPE, COUNT 等
        /// </remarks>
        public static void SortIconList(bool sort = true)
        {
            if (!sort) return;

            static void SortData(AppData data)
            {
                if (data?.MenuList == null || data.AppConfig == null) return;

                SortType sortType = data.AppConfig.IconSortType;
                foreach (MenuInfo menu in data.MenuList)
                {
                    if (menu.IconList == null || menu.IconList.Count <= 1)
                    {
                        continue;
                    }

                    // 保留"添加快捷方式"占位符
                    var placeholder = menu.IconList.FirstOrDefault(icon => icon.IsAddShortcut_NoWrite);
                    List<IconInfo> icons = menu.IconList.Where(icon => !icon.IsAddShortcut_NoWrite).ToList();
                    IEnumerable<IconInfo> ordered = sortType switch
                    {
                        SortType.COUNT_UP => icons.OrderBy(icon => icon.Count),
                        SortType.COUNT_LOW => icons.OrderByDescending(icon => icon.Count),
                        SortType.NAME_UP => icons.OrderBy(icon => icon.Name, StringComparer.OrdinalIgnoreCase),
                        SortType.NAME_LOW => icons.OrderByDescending(icon => icon.Name, StringComparer.OrdinalIgnoreCase),
                        _ => icons,
                    };

                    menu.IconList.Clear();
                    foreach (IconInfo icon in ordered)
                    {
                        menu.IconList.Add(icon);
                    }
                    // 排序后加回占位符
                    if (placeholder != null)
                    {
                        menu.IconList.Add(placeholder);
                    }
                }
            }

            var dataProvider = AppServices.IsConfigured
                ? AppServices.TryGet<Services.Interfaces.IAppDataProvider>()
                : null;
            if (dataProvider != null)
            {
                dataProvider.Write(SortData);
            }
            else
            {
                return;
            }

            var saveService = AppServices.IsConfigured
                ? AppServices.TryGet<Services.Interfaces.ISaveService>()
                : null;
            if (saveService != null)
            {
                saveService.SaveNow();
            }
        }

        public static IconInfo GetIconInfoByPath_NoWrite(string path)
        {
            return GetIconInfoByPath(path);
        }

        public static string GetUniqueUUID()
        {
            return System.Guid.NewGuid().ToString();
        }

        /// <summary>
        /// 为缺少 ImageByteArr 的快捷方式从文件路径补全图标字节。
        /// </summary>
        /// <returns>是否有实际变更</returns>
        private static bool HydrateImportedIconData(AppData appData)
        {
            if (appData.MenuList == null) return false;
            bool changed = false;

            foreach (var icon in ShortcutFolderTreeService.EnumerateIcons(appData.MenuList))
            {
                if (icon == null) continue;

                bool hasBytes = icon.ImageByteArr_NoWrite != null && icon.ImageByteArr_NoWrite.Length > 0;
                bool hasDefault = icon.DefaultImage_NoWrite != null && icon.DefaultImage_NoWrite.Length > 0;
                if (hasBytes || hasDefault || string.IsNullOrWhiteSpace(icon.Path_NoWrite)) continue;

                byte[]? bytes = ImageUtil.GetImageBytesByPath(icon.Path_NoWrite);
                if (bytes != null && bytes.Length > 0)
                {
                    icon.ImageByteArr_NoWrite = bytes;
                    changed = true;
                }
            }
            return changed;
        }

        #endregion
    }
}
