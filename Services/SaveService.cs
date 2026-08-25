using System;
using System.Threading;
using raindesk.Constant;
using raindesk.Services.Interfaces;
using raindesk.Util;

namespace raindesk.Services;

/// <summary>
/// 数据持久化服务实现，封装防抖保存、原子写入和互斥锁保护。
/// 替代 CommonCode 中散落的静态保存方法。
/// </summary>
public sealed class SaveService : ISaveService, IDisposable
{
    private readonly IAppDataProvider _dataProvider;
    private readonly string _filePath;
    private Timer? _debounceTimer;
    private readonly object _debounceLock = new();
    private long _debounceGeneration;
    private readonly SemaphoreSlim _writeMutex = new(1, 1);
    private const int DebounceMs = 500;
    private static readonly TimeSpan MutexWaitTimeout = TimeSpan.FromSeconds(2);
    private bool _disposed;

    public SaveService(IAppDataProvider dataProvider, string filePath)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <inheritdoc/>
    public void SaveNow()
    {
        try
        {
            var snapshot = _dataProvider.GetSnapshot();
            MutexProtectedWrite(_filePath, snapshot.Json);
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "保存数据失败");
        }
    }

    /// <inheritdoc/>
    public void SaveDebounced()
    {
        lock (_debounceLock)
        {
            long generation = ++_debounceGeneration;
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => SaveDebouncedGeneration(generation), null, DebounceMs, Timeout.Infinite);
        }
    }

    private void SaveDebouncedGeneration(long generation)
    {
        if (!_writeMutex.Wait(MutexWaitTimeout))
        {
            LogUtil.WriteErrorLog(new TimeoutException(), "延迟保存等待写入互斥锁超时");
            return;
        }

        try
        {
            lock (_debounceLock)
            {
                if (generation != _debounceGeneration)
                    return;

                _debounceTimer?.Dispose();
                _debounceTimer = null;
            }

            var snapshot = _dataProvider.GetSnapshot();
            SafeFileWriter.WriteAllText(_filePath, snapshot.Json);
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "延迟保存数据失败");
        }
        finally
        {
            _writeMutex.Release();
        }
    }

    /// <inheritdoc/>
    public void CancelPendingSave()
    {
        CancelDebouncedTimer();

        // 等待可能已经进入回调的旧保存退出，超时则记录而不是无限等待。
        if (!_writeMutex.Wait(MutexWaitTimeout))
        {
            LogUtil.WriteErrorLog(new TimeoutException(), "取消待保存时等待写入互斥锁超时");
            return;
        }

        _writeMutex.Release();
    }

    /// <inheritdoc/>
    public async global::System.Threading.Tasks.Task CancelPendingSaveAsync(CancellationToken cancellationToken = default)
    {
        CancelDebouncedTimer();

        try
        {
            await _writeMutex.WaitAsync(MutexWaitTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (TimeoutException ex)
        {
            LogUtil.WriteErrorLog(ex, "取消待保存时等待写入互斥锁超时");
            return;
        }

        _writeMutex.Release();
    }

    /// <inheritdoc/>
    public void Flush()
    {
        CancelDebouncedTimer();

        try
        {
            var snapshot = _dataProvider.GetSnapshot();
            MutexProtectedWrite(_filePath, snapshot.Json);
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "保存数据失败");
        }
    }

    /// <inheritdoc/>
    public async global::System.Threading.Tasks.Task FlushAsync(CancellationToken cancellationToken = default)
    {
        CancelDebouncedTimer();

        try
        {
            var snapshot = _dataProvider.GetSnapshot();
            await MutexProtectedWriteAsync(_filePath, snapshot.Json, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogUtil.WriteErrorLog(ex, "保存数据失败");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CancelDebouncedTimer();

        if (!_writeMutex.Wait(MutexWaitTimeout))
        {
            LogUtil.WriteErrorLog(new TimeoutException(), "释放保存服务时等待写入互斥锁超时");
        }
        else
        {
            _writeMutex.Release();
        }

        _writeMutex.Dispose();
    }

    /// <summary>
    /// 取消当前防抖计时器并使已经排队的回调失效。
    /// 所有立即保存、刷新和释放路径都必须先调用它，避免重复维护竞态处理。
    /// </summary>
    private void CancelDebouncedTimer()
    {
        lock (_debounceLock)
        {
            ++_debounceGeneration;
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }
    }

    // ── 文件写入原语 ──────────────────────────────────────────────

    /// <summary>
    /// 互斥锁保护的写入：同一时刻只有一个写入操作可以执行。
    /// </summary>
    private void MutexProtectedWrite(string filePath, string content)
    {
        if (!_writeMutex.Wait(MutexWaitTimeout))
        {
            LogUtil.WriteErrorLog(new TimeoutException(), "等待写入互斥锁超时");
            return;
        }

        try
        {
            SafeFileWriter.WriteAllText(filePath, content);
        }
        finally
        {
            _writeMutex.Release();
        }
    }

    private async global::System.Threading.Tasks.Task MutexProtectedWriteAsync(string filePath, string content, CancellationToken cancellationToken)
    {
        await _writeMutex.WaitAsync(MutexWaitTimeout, cancellationToken).ConfigureAwait(false);
        try
        {
            SafeFileWriter.WriteAllText(filePath, content);
        }
        finally
        {
            _writeMutex.Release();
        }
    }
}
