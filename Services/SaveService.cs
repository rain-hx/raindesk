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
        _writeMutex.Wait();
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
        lock (_debounceLock)
        {
            ++_debounceGeneration;
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        // 等待可能已经进入回调的旧保存退出。
        _writeMutex.Wait();
        _writeMutex.Release();
    }

    /// <inheritdoc/>
    public void Flush()
    {
        CancelPendingSave();
        SaveNow();
    }

    public void Dispose()
    {
        CancelPendingSave();
        _writeMutex.Dispose();
    }

    // ── 文件写入原语 ──────────────────────────────────────────────

    /// <summary>
    /// 互斥锁保护的写入：同一时刻只有一个写入操作可以执行。
    /// </summary>
    private void MutexProtectedWrite(string filePath, string content)
    {
        _writeMutex.Wait();
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
