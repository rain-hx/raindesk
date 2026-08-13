using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// P0: 拖拽性能计数器。仅在 DEBUG 构建中活跃。
/// 内存计数器，会话结束后输出一次摘要，不在每次事件中写文件。
/// </summary>
internal sealed class LauncherDragPerfCounters
{
    // ── 计数器 ─────────────────────────────────────────────────
    private long _inputEventCount;          // PointerMoved + DragOver 事件总数
    private long _frameUpdateCount;         // 16ms 定时器实际执行次数
    private long _hitTestCount;             // 命中计算次数
    private long _updateLayoutCount;        // UpdateLayout() 调用次数
    private long _collectionMoveCount;      // 集合 Move 次数
    private long _collectionResetCount;     // 集合 Reset 次数（ReplaceAll）
    private long _storyboardCreateCount;    // Storyboard 创建次数
    private long _logCallCount;             // 日志调用次数

    // ── 计时 ─────────────────────────────────────────────────
    private long _pointerToYieldTotalMs;    // 指针到让位反馈累计
    private long _pointerToYieldSamples;    // 指针到让位采样数
    private long _dropToLayoutTotalMs;      // 放手到布局稳定累计
    private long _dropToLayoutSamples;      // 放手到布局采样数

    // ── 会话 ─────────────────────────────────────────────────
    private long _sessionStartTick;
    private long _lastPointerTick;
    private bool _sessionActive;

    // ── 每帧去重 ─────────────────────────────────────────────
    private int _frameHitTestCount;         // 当前帧命中计算次数

    public void BeginSession()
    {
        _sessionStartTick = Stopwatch.GetTimestamp();
        _sessionActive = true;
        _inputEventCount = 0;
        _frameUpdateCount = 0;
        _hitTestCount = 0;
        _updateLayoutCount = 0;
        _collectionMoveCount = 0;
        _collectionResetCount = 0;
        _storyboardCreateCount = 0;
        _logCallCount = 0;
        _pointerToYieldTotalMs = 0;
        _pointerToYieldSamples = 0;
        _dropToLayoutTotalMs = 0;
        _dropToLayoutSamples = 0;
        _frameHitTestCount = 0;
    }

    public void EndSession()
    {
        _sessionActive = false;
    }

    // ── 输入事件 ─────────────────────────────────────────────

    public void RecordInputEvent()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _inputEventCount);
        _lastPointerTick = Stopwatch.GetTimestamp();
    }

    // ── 帧更新 ───────────────────────────────────────────────

    public void RecordFrameUpdate()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _frameUpdateCount);
        // 重置每帧命中计数
        _frameHitTestCount = 0;
    }

    // ── 命中计算 ─────────────────────────────────────────────

    public void RecordHitTest()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _hitTestCount);
        _frameHitTestCount++;
    }

    /// <summary>当前帧是否已执行过命中计算（用于去重）。</summary>
    public bool HasHitTestThisFrame => _frameHitTestCount > 0;

    // ── UpdateLayout ─────────────────────────────────────────

    public void RecordUpdateLayout()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _updateLayoutCount);
    }

    // ── 集合操作 ─────────────────────────────────────────────

    public void RecordCollectionMove()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _collectionMoveCount);
    }

    public void RecordCollectionReset()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _collectionResetCount);
    }

    // ── Storyboard ───────────────────────────────────────────

    public void RecordStoryboardCreate()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _storyboardCreateCount);
    }

    // ── 日志 ─────────────────────────────────────────────────

    public void RecordLogCall()
    {
        if (!_sessionActive) return;
        Interlocked.Increment(ref _logCallCount);
    }

    // ── 计时 ─────────────────────────────────────────────────

    /// <summary>记录从指针进入新目标到让位动画开始的耗时。</summary>
    public void RecordPointerToYieldMs(long ms)
    {
        if (!_sessionActive || ms < 0) return;
        Interlocked.Add(ref _pointerToYieldTotalMs, ms);
        Interlocked.Increment(ref _pointerToYieldSamples);
    }

    /// <summary>记录从放手到最终布局稳定的耗时。</summary>
    public void RecordDropToLayoutMs(long ms)
    {
        if (!_sessionActive || ms < 0) return;
        Interlocked.Add(ref _dropToLayoutTotalMs, ms);
        Interlocked.Increment(ref _dropToLayoutSamples);
    }

    // ── 摘要 ─────────────────────────────────────────────────

    public LauncherDragPerfSummary GetSummary()
    {
        double elapsedSeconds = _sessionActive
            ? (double)(Stopwatch.GetTimestamp() - _sessionStartTick) / Stopwatch.Frequency
            : 0;

        long ptYieldSamples = Interlocked.Read(ref _pointerToYieldSamples);
        long dtLayoutSamples = Interlocked.Read(ref _dropToLayoutSamples);

        return new LauncherDragPerfSummary
        {
            ElapsedSeconds = elapsedSeconds,
            InputEventCount = Interlocked.Read(ref _inputEventCount),
            FrameUpdateCount = Interlocked.Read(ref _frameUpdateCount),
            HitTestCount = Interlocked.Read(ref _hitTestCount),
            UpdateLayoutCount = Interlocked.Read(ref _updateLayoutCount),
            CollectionMoveCount = Interlocked.Read(ref _collectionMoveCount),
            CollectionResetCount = Interlocked.Read(ref _collectionResetCount),
            StoryboardCreateCount = Interlocked.Read(ref _storyboardCreateCount),
            LogCallCount = Interlocked.Read(ref _logCallCount),
            PointerToYieldAvgMs = ptYieldSamples > 0
                ? (double)Interlocked.Read(ref _pointerToYieldTotalMs) / ptYieldSamples
                : 0,
            DropToLayoutAvgMs = dtLayoutSamples > 0
                ? (double)Interlocked.Read(ref _dropToLayoutTotalMs) / dtLayoutSamples
                : 0,
            InputEventsPerSecond = elapsedSeconds > 0
                ? Interlocked.Read(ref _inputEventCount) / elapsedSeconds
                : 0,
            FrameUpdatesPerSecond = elapsedSeconds > 0
                ? Interlocked.Read(ref _frameUpdateCount) / elapsedSeconds
                : 0,
        };
    }

    public string FormatSummary()
    {
        var s = GetSummary();
        return $"[DragPerf] {s.ElapsedSeconds:F1}s | " +
               $"input={s.InputEventCount} ({s.InputEventsPerSecond:F0}/s) " +
               $"frames={s.FrameUpdateCount} ({s.FrameUpdatesPerSecond:F0}/s) " +
               $"hitTests={s.HitTestCount} " +
               $"updateLayout={s.UpdateLayoutCount} " +
               $"moves={s.CollectionMoveCount} resets={s.CollectionResetCount} " +
               $"storyboards={s.StoryboardCreateCount} " +
               $"logs={s.LogCallCount} " +
               $"pointerToYield={s.PointerToYieldAvgMs:F0}ms " +
               $"dropToLayout={s.DropToLayoutAvgMs:F0}ms";
    }
}

internal struct LauncherDragPerfSummary
{
    public double ElapsedSeconds;
    public long InputEventCount;
    public long FrameUpdateCount;
    public long HitTestCount;
    public long UpdateLayoutCount;
    public long CollectionMoveCount;
    public long CollectionResetCount;
    public long StoryboardCreateCount;
    public long LogCallCount;
    public double PointerToYieldAvgMs;
    public double DropToLayoutAvgMs;
    public double InputEventsPerSecond;
    public double FrameUpdatesPerSecond;
}
