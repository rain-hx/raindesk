using System;
using System.Collections.Generic;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// Owns the drag lifecycle: timers, pointer subscriptions, and state.
/// Delegates business-logic steps to page callbacks during incremental migration.
/// </summary>
internal sealed class LauncherDragOrchestrator : IDisposable
{
    private readonly LauncherDragSessionState _state;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly Action _updateDragFromPointer;
    private readonly Action _updatePreviewFromCursor;
    private readonly Action _cleanupDrag;
    private DispatcherQueueTimer? _dragFollowTimer;
    private DispatcherQueueTimer? _batchDropReleaseCleanupTimer;
    private PointerEventHandler? _windowDragPointerMovedHandler;
    private PointerEventHandler? _listViewDragPointerMovedHandler;
    private DragEventHandler? _systemDragSuppressHandler;
    private bool _systemDragSuppressHandlersRegistered;
    private bool _pointerUpdatePending;

    public LauncherDragOrchestrator(
        LauncherDragSessionState state,
        DispatcherQueue dispatcherQueue,
        Action updateDragFromPointer,
        Action updatePreviewFromCursor,
        Action cleanupDrag)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        _updateDragFromPointer = updateDragFromPointer ?? throw new ArgumentNullException(nameof(updateDragFromPointer));
        _updatePreviewFromCursor = updatePreviewFromCursor ?? throw new ArgumentNullException(nameof(updatePreviewFromCursor));
        _cleanupDrag = cleanupDrag ?? throw new ArgumentNullException(nameof(cleanupDrag));
    }

    /// <summary>Expose the drag session state.</summary>
    public LauncherDragSessionState State => _state;

    /// <summary>The batch drop release cleanup timer, if active.</summary>
    public DispatcherQueueTimer? BatchDropReleaseCleanupTimer => _batchDropReleaseCleanupTimer;

    // ── Pointer tracking lifecycle ──────────────────────────────────

    /// <summary>
    /// Begin tracking pointer movement during drag.
    /// Subscribes PointerMoved on window and ListView, registers system drag
    /// visual suppression, and starts the 16ms follow timer.
    /// </summary>
    public void BeginPointerTracking(
        FrameworkElement? listView,
        UIElement? windowRoot,
        Action<UIElement, PointerRoutedEventArgs> onPointerMoved,
        Action<UIElement, DragEventArgs> onSystemDragSuppress)
    {
        _listViewDragPointerMovedHandler = (s, e) =>
        {
            _pointerUpdatePending = true;
            onPointerMoved((UIElement)s!, e);
        };
        _windowDragPointerMovedHandler = (s, e) =>
        {
            _pointerUpdatePending = true;
            onPointerMoved((UIElement)s!, e);
        };
        _systemDragSuppressHandler = (s, e) => onSystemDragSuppress((UIElement)s!, e);

        if (listView != null)
        {
            listView.AddHandler(UIElement.PointerMovedEvent, _listViewDragPointerMovedHandler, true);
        }

        if (windowRoot != null)
        {
            windowRoot.AddHandler(UIElement.PointerMovedEvent, _windowDragPointerMovedHandler, true);
        }

        RegisterSystemDragVisualSuppression(listView, windowRoot);
        _pointerUpdatePending = true;
        EnsureDragFollowTimer();
    }

    /// <summary>
    /// End all pointer tracking: removes handlers, unregisters suppression,
    /// and stops the follow timer.
    /// </summary>
    public void EndPointerTracking(
        FrameworkElement? listView,
        UIElement? windowRoot)
    {
        if (listView != null && _listViewDragPointerMovedHandler != null)
        {
            listView.RemoveHandler(UIElement.PointerMovedEvent, _listViewDragPointerMovedHandler);
        }

        if (windowRoot != null && _windowDragPointerMovedHandler != null)
        {
            windowRoot.RemoveHandler(UIElement.PointerMovedEvent, _windowDragPointerMovedHandler);
        }

        _listViewDragPointerMovedHandler = null;
        _windowDragPointerMovedHandler = null;

        UnregisterSystemDragVisualSuppression(listView, windowRoot);
        _pointerUpdatePending = false;
        StopDragFollowTimer();
    }

    // ── Timer management ────────────────────────────────────────────

    /// <summary>Create and start the 16ms drag follow timer.</summary>
    public void EnsureDragFollowTimer()
    {
        if (_dragFollowTimer != null) return;

        _dragFollowTimer = _dispatcherQueue.CreateTimer();
        _dragFollowTimer.Interval = TimeSpan.FromMilliseconds(16);
        _dragFollowTimer.Tick += DragFollowTimer_Tick;
        _dragFollowTimer.Start();
    }

    /// <summary>Stop and dispose the drag follow timer.</summary>
    public void StopDragFollowTimer()
    {
        if (_dragFollowTimer == null) return;
        _dragFollowTimer.Stop();
        _dragFollowTimer.Tick -= DragFollowTimer_Tick;
        _dragFollowTimer = null;
    }

    /// <summary>Create and start the batch drop release cleanup timer.</summary>
    public void StartBatchDropReleaseCleanup(TimeSpan delay, List<IconInfo> icons, Action onTick)
    {
        StopBatchDropReleaseCleanup();
        _batchDropReleaseCleanupTimer = _dispatcherQueue.CreateTimer();
        _batchDropReleaseCleanupTimer.Interval = delay;
        _batchDropReleaseCleanupTimer.Tick += (s, a) =>
        {
            StopBatchDropReleaseCleanup();
            onTick();
        };
        _batchDropReleaseCleanupTimer.Start();
    }

    /// <summary>Stop the batch drop release cleanup timer.</summary>
    public void StopBatchDropReleaseCleanup()
    {
        if (_batchDropReleaseCleanupTimer == null) return;
        _batchDropReleaseCleanupTimer.Stop();
        _batchDropReleaseCleanupTimer = null;
    }

    // ── System drag visual suppression ──────────────────────────────

    public void RegisterSystemDragVisualSuppression(
        FrameworkElement? listView,
        UIElement? windowRoot)
    {
        if (_systemDragSuppressHandlersRegistered) return;
        if (_systemDragSuppressHandler == null) return;

        if (listView != null)
        {
            listView.AddHandler(UIElement.DragEnterEvent, _systemDragSuppressHandler, true);
            listView.AddHandler(UIElement.DragOverEvent, _systemDragSuppressHandler, true);
        }

        if (windowRoot != null)
        {
            windowRoot.AddHandler(UIElement.DragEnterEvent, _systemDragSuppressHandler, true);
            windowRoot.AddHandler(UIElement.DragOverEvent, _systemDragSuppressHandler, true);
        }

        _systemDragSuppressHandlersRegistered = true;
    }

    public void UnregisterSystemDragVisualSuppression(
        FrameworkElement? listView,
        UIElement? windowRoot)
    {
        if (!_systemDragSuppressHandlersRegistered) return;
        if (_systemDragSuppressHandler == null) return;

        if (listView != null)
        {
            listView.RemoveHandler(UIElement.DragEnterEvent, _systemDragSuppressHandler);
            listView.RemoveHandler(UIElement.DragOverEvent, _systemDragSuppressHandler);
        }

        if (windowRoot != null)
        {
            windowRoot.RemoveHandler(UIElement.DragEnterEvent, _systemDragSuppressHandler);
            windowRoot.RemoveHandler(UIElement.DragOverEvent, _systemDragSuppressHandler);
        }

        _systemDragSuppressHandlersRegistered = false;
    }

    // ── Full cleanup ────────────────────────────────────────────────

    /// <summary>Complete cleanup: stop timers, remove handlers, reset state.</summary>
    public void Cleanup(
        FrameworkElement? listView,
        UIElement? windowRoot)
    {
        EndPointerTracking(listView, windowRoot);
        StopBatchDropReleaseCleanup();
        _cleanupDrag();
        _state.Reset();
    }

    public void Dispose()
    {
        StopDragFollowTimer();
        StopBatchDropReleaseCleanup();
    }

    // ── Timer tick handler ──────────────────────────────────────────

    private void DragFollowTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (!_state.IsCustomDragActive) return;

        if (_pointerUpdatePending)
        {
            _pointerUpdatePending = false;
            _updateDragFromPointer();
        }

        _updatePreviewFromCursor();
    }
}
