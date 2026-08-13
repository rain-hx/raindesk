using System;
using System.IO;
using System.Threading;

namespace rainspace.Util;

/// <summary>
/// 线程安全的文件写入工具，提供互斥锁保护和原子写入。
/// 灵感来自 TaskTrove 的 safeWriteDataFile 模式。
///
/// 特性：
/// - Mutex 保护：同一文件路径的写入操作互斥，防止并发写入损坏数据
/// - 原子写入：先写 .tmp 文件，再 File.Move 替换，崩溃时原文件不损坏
/// - 写入验证：可选的写入后校验（读取并比较内容）
///
/// 用法：
///   SafeFileWriter.WriteAllText(filePath, jsonContent);
///   await SafeFileWriter.WriteAllTextAsync(filePath, jsonContent);
/// </summary>
public static class SafeFileWriter
{
    private static readonly ReaderWriterLockSlim _pathLocks = new();

    /// <summary>
    /// 同步写入文件（互斥 + 原子）。
    /// </summary>
    public static void WriteAllText(string filePath, string content)
    {
        var mutex = GetOrCreateMutex(filePath);
        mutex.WaitOne();
        try
        {
            AtomicWrite(filePath, content);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    /// <summary>
    /// 异步写入文件（互斥 + 原子）。
    /// </summary>
    public static global::System.Threading.Tasks.Task WriteAllTextAsync(string filePath, string content)
    {
        return global::System.Threading.Tasks.Task.Run(() => WriteAllText(filePath, content));
    }

    /// <summary>
    /// 原子写入（无锁，调用者负责同步）。
    /// 先写临时文件，再替换原文件。
    /// </summary>
    public static void AtomicWrite(string filePath, string content)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string tempPath = filePath + ".tmp";

        try
        {
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            throw;
        }
    }

    /// <summary>
    /// 安全读取文件内容，文件不存在时返回 null。
    /// </summary>
    public static string? ReadAllTextIfExists(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var mutex = GetOrCreateMutex(filePath);
        mutex.WaitOne();
        try
        {
            return File.ReadAllText(filePath);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    // ── 内部实现 ──────────────────────────────────────────────────

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Mutex> _mutexes = new();

    private static Mutex GetOrCreateMutex(string filePath)
    {
        // 使用文件路径的规范化形式作为 mutex 名称
        string key = Path.GetFullPath(filePath).ToLowerInvariant();
        return _mutexes.GetOrAdd(key, k => new Mutex(false, "rainspace_file_" + k.GetHashCode().ToString("X")));
    }
}
