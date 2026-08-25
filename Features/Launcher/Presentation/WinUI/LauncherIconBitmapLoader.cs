using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using raindesk.Util;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// 后台解析图标字节、异步解码 BitmapImage，并在 UI 线程分批提交，
/// 避免“全部图标”模式一次性同步解码大量图标导致卡顿。
/// </summary>
internal sealed class LauncherIconBitmapLoader : IDisposable
{
    private readonly DispatcherQueue _dispatcher;
    private readonly ConcurrentQueue<IconInfo> _pending = new();
    private readonly ConcurrentQueue<(IconInfo Icon, byte[]? Bytes)> _results = new();
    private readonly HashSet<IconInfo> _inFlight = new();
    private readonly object _gate = new();
    private int _drainScheduled;
    private bool _disposed;
    private const int DrainBatch = 10;

    public LauncherIconBitmapLoader(DispatcherQueue dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>将尚未解码的图标加入后台解码队列（须在 UI 线程调用）。</summary>
    public void Enqueue(IEnumerable<IconInfo>? icons)
    {
        if (icons == null || _disposed)
        {
            return;
        }

        bool any = false;
        lock (_gate)
        {
            foreach (IconInfo? icon in icons)
            {
                if (icon == null || icon.IsAddShortcut_NoWrite)
                {
                    continue;
                }
                if (icon.HasCachedBitmap_NoWrite)
                {
                    continue;
                }
                if (_inFlight.Add(icon))
                {
                    _pending.Enqueue(icon);
                    any = true;
                }
            }
        }

        if (any)
        {
            StartWorker();
        }
    }

    public void Dispose()
    {
        // 清空挂起与在途任务，让后台线程安全退出。
        _disposed = true;
        lock (_gate)
        {
            _pending.Clear();
        }
    }

    /// <summary>控件重新挂载后恢复可用状态。</summary>
    public void Reset()
    {
        _disposed = false;
        lock (_gate)
        {
            _inFlight.Clear();
            _pending.Clear();
        }
        while (_results.TryDequeue(out _)) { }
    }

    private void StartWorker()
    {
        _ = global::System.Threading.Tasks.Task.Run(WorkerLoop);
    }

    private void WorkerLoop()
    {
        try
        {
            while (true)
            {
                IconInfo? icon;
                lock (_gate)
                {
                    if (_disposed || !_pending.TryDequeue(out icon))
                    {
                        return;
                    }
                }

                if (icon == null)
                {
                    continue;
                }

                byte[]? bytes = ResolveBytes(icon);
                _results.Enqueue((icon, bytes));
                ScheduleDrain();
            }
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "后台解析图标字节失败");
        }
    }

    private void ScheduleDrain()
    {
        if (Interlocked.Exchange(ref _drainScheduled, 1) == 1)
        {
            return;
        }

        if (!_dispatcher.TryEnqueue(async () =>
            {
                Interlocked.Exchange(ref _drainScheduled, 0);
                await DrainAsync().ConfigureAwait(true);
            }))
        {
            Interlocked.Exchange(ref _drainScheduled, 0);
        }
    }

    private async global::System.Threading.Tasks.Task DrainAsync()
    {
        var snapshot = new List<(IconInfo Icon, byte[]? Bytes)>();
        for (int i = 0; i < DrainBatch; i++)
        {
            if (_results.TryDequeue(out (IconInfo Icon, byte[]? Bytes) result))
            {
                snapshot.Add(result);
            }
            else
            {
                break;
            }
        }

        foreach ((IconInfo icon, byte[]? bytes) in snapshot)
        {
            lock (_gate)
            {
                _inFlight.Remove(icon);
            }

            if (bytes == null || bytes.Length == 0)
            {
                continue;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.DecodePixelWidth = 128;
                using (var stream = new MemoryStream(bytes))
                {
                    await bitmap.SetSourceAsync(stream.AsRandomAccessStream()).AsTask().ConfigureAwait(true);
                }

                icon.ImageByteArr_NoWrite = bytes;
                icon.SetBitmapImageDirect(bitmap);
            }
            catch (Exception ex)
            {
                LogUtil.WriteErrorLog(ex, "后台解码图标失败");
            }
        }

        if (!_results.IsEmpty)
        {
            ScheduleDrain();
        }
    }

    private static byte[]? ResolveBytes(IconInfo icon)
    {
        byte[]? bytes = icon.ImageByteArr_NoWrite;
        if (bytes == null || bytes.Length == 0)
        {
            bytes = icon.DefaultImage_NoWrite;
        }

        if ((bytes == null || bytes.Length == 0) && !string.IsNullOrWhiteSpace(icon.Path_NoWrite))
        {
            bytes = ImageUtil.GetImageBytesByPath(icon.Path_NoWrite);
        }

        return bytes;
    }
}
