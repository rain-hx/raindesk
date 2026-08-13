using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using raindesk.Constant;
using raindesk.ViewModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace raindesk.Util
{
    public static class ImageUtil
    {
        private static readonly string SYSTEM_ITEM = "::{.*}";

        // LRU 缓存：使用 LinkedList 维护访问顺序，ConcurrentDictionary 提供 O(1) 查找
        private static readonly object _cacheLock = new();
        private static readonly LinkedList<string> _lruOrder = new();
        private static readonly Dictionary<string, (BitmapImage Image, LinkedListNode<string> Node)> _imageCache = new();
        private static readonly ConcurrentDictionary<string, long> _shortcutRefreshStamps =
            new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, byte> _shortcutRefreshesInFlight =
            new(StringComparer.OrdinalIgnoreCase);
        private const int MaxCacheEntries = 200;

        /// <summary>
        /// byte[] 转 BitmapImage（默认解码为 128px 宽，节省内存）
        /// </summary>
        public static BitmapImage ByteArrToImage(byte[] array, int decodePixelWidth = 128)
        {
            if (array == null || array.Length == 0) return null;
            try
            {
                var bi = new BitmapImage();
                // PERF: 设置 DecodePixelWidth 避免解码为原始大尺寸，节省内存
                if (decodePixelWidth > 0)
                {
                    bi.DecodePixelWidth = decodePixelWidth;
                }
                // SetSource 是同步调用，读取完成后流不再需要
                using (var ms = new MemoryStream(array))
                {
                    bi.SetSource(ms.AsRandomAccessStream());
                }
                return bi;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "ByteArrToImage 失败");
                return null;
            }
        }

        /// <summary>
        /// BitmapImage 转 byte[]（通过 RenderTargetBitmap）
        /// </summary>
        public static byte[] BitmapImageToByte(BitmapImage bi)
        {
            if (bi == null) return null;
            try
            {
                // BitmapImage 在 WinUI 3 中无法直接编码为 PNG
                // 返回 null 让调用方跳过此路径，直接使用 byte[] 流程
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Base64 转 BitmapImage
        /// </summary>
        public static BitmapImage Base64ToBitmapImage(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                return ByteArrToImage(bytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取文件图标（核心方法）
        /// - 图片文件：直接加载
        /// - exe/dll/ico 等：用 FileIcon 提取图标
        /// - 目录：返回默认图标
        /// </summary>
        public static BitmapImage GetBitmapIconByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            filePath = NormalizeEnvironmentPath(filePath);
            filePath = ResolveShortcutIconPathForDisplay(filePath);
            filePath = NormalizeEnvironmentPath(filePath);

            // 缓存查找（LRU：命中时移到队首）
            lock (_cacheLock)
            {
                if (_imageCache.TryGetValue(filePath, out var cached))
                {
                    _lruOrder.Remove(cached.Node);
                    _lruOrder.AddFirst(cached.Node);
                    return cached.Image;
                }
            }

            try
            {
                if (File.Exists(filePath) || IsSystemItem(filePath))
                {
                    BitmapImage bi;
                    if (IsImage(filePath))
                    {
                        // 图片文件直接加载
                        bi = LoadImageFile(filePath);
                    }
                    else
                    {
                        // exe/dll/lnk/其他文件：提取图标
                        bi = GetIconFromFile(filePath);
                    }

                    if (bi != null)
                        CacheImage(filePath, bi);
                    return bi;
                }
                else if (Directory.Exists(filePath))
                {
                    // 目录：返回默认图标
                    return Base64ToBitmapImage(Constants.DEFAULT_DIR_IMAGE_BASE64);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取可持久化的图标字节。
        /// </summary>
        public static byte[] GetImageBytesByPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            filePath = NormalizeEnvironmentPath(filePath);
            filePath = ResolveShortcutIconPathForDisplay(filePath);
            filePath = NormalizeEnvironmentPath(filePath);

            try
            {
                if (File.Exists(filePath))
                {
                    return IsImage(filePath)
                        ? File.ReadAllBytes(filePath)
                        : FileIcon.GetIconBytes(filePath);
                }

                if (Directory.Exists(filePath))
                {
                    return Convert.FromBase64String(Constants.DEFAULT_DIR_IMAGE_BASE64);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 获取文件图标（支持 .lnk 解析）
        /// </summary>
        public static BitmapImage GetBitmapIconByUnknownPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            string ext = "";
            if (!IsSystemItem(path))
            {
                ext = Path.GetExtension(path).ToLower();
            }

            string iconPath = null;
            if (".lnk".Equals(ext))
            {
                try
                {
                    string targetPath = FileUtil.GetTargetPathByLnk(path);
                    iconPath = FileUtil.GetIconPathByLnk(path);
                    if (targetPath != null)
                    {
                        path = targetPath;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                iconPath = path;
            }

            return GetBitmapIconByPath(iconPath);
        }

        private static string NormalizeEnvironmentPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            return path.Contains("%windir%", StringComparison.OrdinalIgnoreCase)
                ? path.Replace("%windir%", Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows", StringComparison.OrdinalIgnoreCase)
                : path;
        }

        private static bool IsShortcutPath(string? path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && ".lnk".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveShortcutIconPathForDisplay(string filePath)
        {
            if (!IsShortcutPath(filePath))
            {
                return filePath;
            }

            try
            {
                string iconPath = FileUtil.GetIconPathByLnk(filePath);
                if (!string.IsNullOrWhiteSpace(iconPath))
                {
                    return iconPath;
                }

                string targetPath = FileUtil.GetTargetPathByLnk(filePath);
                if (!string.IsNullOrWhiteSpace(targetPath))
                {
                    return targetPath;
                }
            }
            catch { }

            return filePath;
        }

        private static string? GetShortcutRefreshPath(IconInfo icon)
        {
            if (icon == null)
            {
                return null;
            }

            if (IsShortcutPath(icon.LnkPath_NoWrite))
            {
                return icon.LnkPath_NoWrite;
            }

            return IsShortcutPath(icon.Path_NoWrite) ? icon.Path_NoWrite : null;
        }

        private static bool TryRefreshShortcutIconBitmap(IconInfo icon)
        {
            string? shortcutPath = GetShortcutRefreshPath(icon);
            if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
            {
                return false;
            }

            byte[]? liveBytes = GetImageBytesByPath(shortcutPath);
            if (liveBytes == null || liveBytes.Length == 0)
            {
                return false;
            }

            BitmapImage? bitmap = ByteArrToImage(liveBytes);
            if (bitmap == null)
            {
                return false;
            }

            icon.ImageByteArr_NoWrite = liveBytes;
            icon.DefaultImage_NoWrite = liveBytes;
            icon.SetBitmapImageDirect(bitmap);
            return true;
        }

        private static bool ShortcutIconNeedsRefresh(
            string shortcutPath,
            bool hasCachedBitmap,
            out bool deferRefresh)
        {
            deferRefresh = false;
            try
            {
                long stamp = File.GetLastWriteTimeUtc(shortcutPath).Ticks;
                if (!_shortcutRefreshStamps.TryGetValue(shortcutPath, out long previous))
                {
                    _shortcutRefreshStamps[shortcutPath] = stamp;
                    // Persisted icon bytes are already suitable for the first frame.
                    // Refresh them in the background to remove legacy .lnk overlays.
                    deferRefresh = hasCachedBitmap;
                    return !hasCachedBitmap;
                }

                if (previous == stamp)
                {
                    return false;
                }

                _shortcutRefreshStamps[shortcutPath] = stamp;
                return true;
            }
            catch
            {
                return true;
            }
        }

        private static void QueueShortcutIconRefresh(
            IconInfo icon,
            string shortcutPath,
            Microsoft.UI.Dispatching.DispatcherQueue dispatcher)
        {
            if (!_shortcutRefreshesInFlight.TryAdd(shortcutPath, 0))
            {
                return;
            }

            _ = System.Threading.Tasks.Task.Run(() => GetImageBytesByPath(shortcutPath)).ContinueWith(task =>
            {
                byte[]? bytes = task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion ? task.Result : null;
                if (bytes == null || bytes.Length == 0)
                {
                    _shortcutRefreshesInFlight.TryRemove(shortcutPath, out _);
                    return;
                }

                if (!dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        BitmapImage? bitmap = ByteArrToImage(bytes);
                        if (bitmap != null)
                        {
                            icon.ImageByteArr_NoWrite = bytes;
                            icon.DefaultImage_NoWrite = bytes;
                            icon.SetBitmapImageDirect(bitmap);
                        }
                    }
                    finally
                    {
                        _shortcutRefreshesInFlight.TryRemove(shortcutPath, out _);
                    }
                }))
                {
                    _shortcutRefreshesInFlight.TryRemove(shortcutPath, out _);
                }
            }, System.Threading.Tasks.TaskScheduler.Default);
        }

        /// <summary>
        /// 从文件提取图标（exe/dll/lnk 等非图片文件）
        /// </summary>
        private static BitmapImage GetIconFromFile(string filePath)
        {
            try
            {
                byte[] iconBytes = FileIcon.GetIconBytes(filePath);
                if (iconBytes != null && iconBytes.Length > 0)
                {
                    return ByteArrToImage(iconBytes);
                }
            }
            catch { }

            // 最后回退：尝试用 UriSource 加载
            try
            {
                if (File.Exists(filePath))
                {
                    var bi = new BitmapImage();
                    bi.UriSource = new Uri(filePath);
                    return bi;
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// 加载图片文件（解码为 128px 宽，节省内存）
        /// </summary>
        private static BitmapImage LoadImageFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                byte[] bytes = File.ReadAllBytes(filePath);
                if (bytes.Length == 0) return null;
                var bi = new BitmapImage();
                // PERF: 设置 DecodePixelWidth 避免解码为原始大尺寸，节省内存
                bi.DecodePixelWidth = 128;
                using (var ms = new MemoryStream(bytes))
                {
                    bi.SetSource(ms.AsRandomAccessStream());
                }
                return bi;
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "LoadImageFile 失败: " + filePath);
                return null;
            }
        }

        /// <summary>
        /// 判断文件是否为图片
        /// </summary>
        private static readonly HashSet<string> _imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bmp", "jpg", "jpeg", "png", "tif", "gif", "pcx", "tga", "exif", "fpx",
            "svg", "psd", "cdr", "pcd", "dxf", "ufo", "eps", "ai", "raw", "wmf",
            "webp", "avif", "ico"
        };

        public static bool IsImage(string path)
        {
            try
            {
                string ext = Path.GetExtension(path);
                if (string.IsNullOrEmpty(ext) || ext.Length < 2) return false;
                string suffix = ext.Substring(1).ToLower();
                return _imageExtensions.Contains(suffix);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为系统项（如 ::{20D04FE0...}）
        /// </summary>
        public static bool IsSystemItem(string path)
        {
            return Regex.IsMatch(path, SYSTEM_ITEM);
        }

        private static void CacheImage(string key, BitmapImage bi)
        {
            lock (_cacheLock)
            {
                // 如果已存在，更新并移到队首
                if (_imageCache.TryGetValue(key, out var existing))
                {
                    _lruOrder.Remove(existing.Node);
                    var node = _lruOrder.AddFirst(key);
                    _imageCache[key] = (bi, node);
                    return;
                }

                // 淘汰最久未使用的条目（队尾）
                while (_imageCache.Count >= MaxCacheEntries)
                {
                    var oldest = _lruOrder.Last;
                    if (oldest == null) break;
                    _lruOrder.RemoveLast();
                    _imageCache.Remove(oldest.Value);
                }

                // 插入新条目到队首
                var newNode = _lruOrder.AddFirst(key);
                _imageCache[key] = (bi, newNode);
            }
        }

        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _imageCache.Clear();
                _lruOrder.Clear();
            }

            _shortcutRefreshStamps.Clear();
            _shortcutRefreshesInFlight.Clear();
        }

        /// <summary>
        /// 确保单个快捷方式图标已解码（须在 UI 线程调用）。
        /// </summary>
        public static void EnsureIconBitmap(IconInfo icon)
        {
            if (icon == null || icon.IsAddShortcut_NoWrite) return;

            try
            {
                BitmapImage? cachedBitmap = icon.BitmapImage_NoWrite;
                string? shortcutPath = GetShortcutRefreshPath(icon);
                bool deferShortcutRefresh = false;
                if (!string.IsNullOrWhiteSpace(shortcutPath)
                    && ShortcutIconNeedsRefresh(
                        shortcutPath,
                        cachedBitmap != null,
                        out deferShortcutRefresh)
                    && TryRefreshShortcutIconBitmap(icon))
                {
                    return;
                }

                if (deferShortcutRefresh
                    && Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() is { } dispatcher)
                {
                    QueueShortcutIconRefresh(icon, shortcutPath!, dispatcher);
                }

                if (!string.IsNullOrWhiteSpace(icon.Path_NoWrite) && File.Exists(icon.Path_NoWrite))
                {
                    string ext = Path.GetExtension(icon.Path_NoWrite).ToLowerInvariant();
                    if (ext is ".bat" or ".cmd" or ".ps1" or ".vbs" or ".wsf" or ".sh")
                    {
                        var liveBytes = GetImageBytesByPath(icon.Path_NoWrite);
                        if (liveBytes != null && liveBytes.Length > 0)
                        {
                            icon.ImageByteArr_NoWrite = liveBytes;
                            icon.SetBitmapImageDirect(ByteArrToImage(liveBytes));
                            return;
                        }
                    }
                }

                if (cachedBitmap != null) return;

                byte[]? bytes = icon.ImageByteArr_NoWrite;
                if (bytes == null || bytes.Length == 0)
                    bytes = icon.DefaultImage_NoWrite;
                if ((bytes == null || bytes.Length == 0) && !string.IsNullOrWhiteSpace(icon.Path_NoWrite))
                {
                    bytes = GetImageBytesByPath(icon.Path_NoWrite);
                    if (bytes != null && bytes.Length > 0)
                        icon.ImageByteArr_NoWrite = bytes;
                }

                var bi = icon.BitmapImage_NoWrite;
                if (bi != null)
                    icon.SetBitmapImageDirect(bi);
            }
            catch { }
        }

        /// <summary>
        /// 在 UI 线程批量预加载图标，供 x:Bind 绑定刷新。
        /// </summary>
        public static void PreloadImages(IEnumerable<MenuInfo> menuList)
        {
            if (menuList == null) return;

            foreach (var menu in menuList)
            {
                if (menu?.IconList == null) continue;
                foreach (var icon in menu.IconList)
                {
                    EnsureIconBitmap(icon);
                }
            }
        }
    }
}
