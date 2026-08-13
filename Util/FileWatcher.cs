using raindesk.ViewModel;
using System;
using System.IO;

namespace raindesk.Util
{
    /// <summary>
    /// 文件系统监视器，监视目录变化并触发通知。
    /// </summary>
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher? _watcher;
        private bool _disposed;

        public event EventHandler<string>? FileChanged;

        public FileWatcher(string path, string filter = "*.*")
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    LogUtil.WriteLog($"FileWatcher: 目录不存在 {path}");
                    return;
                }

                _watcher = new FileSystemWatcher(path, filter)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _watcher.Changed += OnFileEvent;
                _watcher.Created += OnFileEvent;
                _watcher.Deleted += OnFileEvent;
                _watcher.Renamed += (s, e) => FileChanged?.Invoke(this, e.FullPath);
                _watcher.Error += (s, e) => LogUtil.WriteErrorLog(e.GetException(), "FileWatcher 错误");
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "FileWatcher 初始化失败: " + path);
            }
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                FileChanged?.Invoke(this, e.FullPath);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "FileWatcher 事件处理异常");
            }
        }

        public static FileWatcher? LinkMenuWatcher(MenuInfo? menuInfo)
        {
            string? path = menuInfo?.LinkPath;
            if (string.IsNullOrEmpty(path)) return null;
            return new FileWatcher(path, "*.*");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _watcher?.Dispose();
                _watcher = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
