using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using rainspace.Controls;
using rainspace.Features.Workspace.Presentation;
using Windows.Foundation;

namespace rainspace.Features.Workspace.Presentation.WinUI;

/// <summary>
/// Workspace 分组选择指示器的更新合并、重试调度、几何缓存和 Storyboard 所有者。
/// </summary>
internal sealed class WorkspaceGroupTabSelectionIndicatorController
{
    private bool _initialized;
    private bool _updateQueued;
    private int _pendingSelectionIndex = -1;
    private bool _pendingAnimate;
    private Storyboard? _storyboard;
    private double _currentY;
    private double _currentHeight;

    public bool BeginRefresh(bool animationsEnabled)
    {
        bool animate = _initialized && animationsEnabled;
        _initialized = true;
        return animate;
    }

    public void Schedule(
        DispatcherQueue dispatcherQueue,
        int selectedIndex,
        bool animate,
        int attempt,
        Action<int, bool, int> apply)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);
        ArgumentNullException.ThrowIfNull(apply);
        if (attempt == 0)
        {
            _pendingSelectionIndex = selectedIndex;
            _pendingAnimate = animate;
            if (_updateQueued)
            {
                return;
            }

            _updateQueued = true;
            bool queued = dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
            {
                _updateQueued = false;
                int pendingIndex = _pendingSelectionIndex;
                bool pendingAnimate = _pendingAnimate;
                _pendingSelectionIndex = -1;
                _pendingAnimate = false;
                apply(pendingIndex, pendingAnimate, 0);
            });
            if (!queued)
            {
                _updateQueued = false;
            }

            return;
        }

        dispatcherQueue.TryEnqueue(
            DispatcherQueuePriority.Low,
            () => apply(selectedIndex, animate, attempt));
    }

    public void ScheduleUpdate<T>(
        DispatcherQueue dispatcherQueue,
        int selectedIndex,
        bool animate,
        IReadOnlyList<T> tabs,
        ListView? listView,
        ScrollViewer? scrollViewer,
        FrameworkElement? indicator,
        Func<T, bool> isEditing,
        WorkspaceGroupTabWidthMetrics widthMetrics,
        bool animationsAllowed,
        TimeSpan duration,
        int attempt = 0,
        int maxAttempts = 10)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(tabs);
        ArgumentNullException.ThrowIfNull(isEditing);
        Schedule(
            dispatcherQueue,
            selectedIndex,
            animate,
            attempt,
            (pendingIndex, pendingAnimate, pendingAttempt) => ApplyUpdate(
                dispatcherQueue,
                pendingIndex,
                pendingAnimate,
                pendingAttempt,
                maxAttempts,
                tabs,
                listView,
                scrollViewer,
                indicator,
                isEditing,
                widthMetrics,
                animationsAllowed,
                duration));
    }

    public void Update(
        FrameworkElement indicator,
        Rect bounds,
        bool animate,
        bool animationsAllowed,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(indicator);
        double targetY = bounds.Y;
        double targetHeight = bounds.Height;
        indicator.Visibility = Visibility.Visible;

        if (!animate || !animationsAllowed)
        {
            StopStoryboard();
            ApplyLayout(indicator, targetY, targetHeight);
            return;
        }

        double canvasTop = Canvas.GetTop(indicator);
        double startY = _currentHeight > 0d
            ? _currentY
            : double.IsNaN(canvasTop) ? _currentY : canvasTop;
        double startHeight = _currentHeight > 0d ? _currentHeight : indicator.Height;
        if (startHeight <= 0d)
        {
            startHeight = targetHeight;
        }

        if (Math.Abs(startY - targetY) < 0.5d
            && Math.Abs(startHeight - targetHeight) < 0.5d)
        {
            ApplyLayout(indicator, targetY, targetHeight);
            return;
        }

        Animate(indicator, startY, startHeight, targetY, targetHeight, duration);
    }

    private void ApplyUpdate<T>(
        DispatcherQueue dispatcherQueue,
        int selectedIndex,
        bool animate,
        int attempt,
        int maxAttempts,
        IReadOnlyList<T> tabs,
        ListView? listView,
        ScrollViewer? scrollViewer,
        FrameworkElement? indicator,
        Func<T, bool> isEditing,
        WorkspaceGroupTabWidthMetrics widthMetrics,
        bool animationsAllowed,
        TimeSpan duration)
        where T : class
    {
        T? tab = WorkspaceGroupSelectionProjection.ResolveTab(tabs, selectedIndex);
        if (tab == null || listView == null)
        {
            return;
        }

        int tabIndex = WorkspaceGroupSelectionProjection.ToTabIndex(selectedIndex);
        listView.UpdateLayout();
        UIElement? host = listView.Parent as UIElement;
        if (host is FrameworkElement frameworkHost)
        {
            frameworkHost.UpdateLayout();
        }

        bool editing = isEditing(tab);
        if (editing
            && WorkspaceGroupTabLayoutAdapter.TryGetContainer(
                listView,
                tab,
                out ListViewItem editContainer))
        {
            WorkspaceGroupTabSizingAdapter.ApplyEditWidth(editContainer, widthMetrics);
        }

        if (host == null
            || !WorkspaceGroupTabLayoutAdapter.TryGetBounds(
                listView,
                tab,
                host,
                out Rect bounds))
        {
            if (ShouldRetry(attempt, maxAttempts))
            {
                ScheduleUpdate(
                    dispatcherQueue,
                    selectedIndex,
                    animate,
                    tabs,
                    listView,
                    scrollViewer,
                    indicator,
                    isEditing,
                    widthMetrics,
                    animationsAllowed,
                    duration,
                    attempt + 1,
                    maxAttempts);
            }

            return;
        }

        bool useAnimate = ShouldAnimate(
            animate,
            attempt,
            editing,
            animationsAllowed);
        ScrollIntoView(listView, scrollViewer, tabIndex, useAnimate);
        if (indicator != null)
        {
            Update(
                indicator,
                bounds,
                useAnimate,
                animationsAllowed,
                duration);
        }
    }

    private static void ScrollIntoView(
        ListView listView,
        ScrollViewer? scrollViewer,
        int tabIndex,
        bool animate)
    {
        if (scrollViewer == null || tabIndex < 0)
        {
            return;
        }

        listView.UpdateLayout();
        if (!WorkspaceGroupTabLayoutAdapter.TryGetBounds(
                listView,
                tabIndex,
                out Rect bounds))
        {
            return;
        }

        WorkspaceGroupTabScrollPlan plan = WorkspaceGroupTabScrollPlanner.Plan(
            bounds.Y,
            bounds.Height,
            scrollViewer.VerticalOffset,
            scrollViewer.ViewportHeight,
            scrollViewer.ScrollableHeight);
        if (plan.ShouldScroll)
        {
            scrollViewer.ChangeView(
                null,
                plan.TargetOffset,
                null,
                disableAnimation: !animate);
        }
    }

    internal static bool ShouldRetry(int attempt, int maxAttempts) =>
        attempt < Math.Max(0, maxAttempts);

    internal static bool ShouldAnimate(
        bool requested,
        int attempt,
        bool editing,
        bool animationsAllowed) =>
        requested && attempt == 0 && !editing && animationsAllowed;

    private void ApplyLayout(FrameworkElement indicator, double y, double height)
    {
        _currentY = y;
        _currentHeight = height;
        Canvas.SetTop(indicator, y);
        indicator.Height = height;
    }

    private void Animate(
        FrameworkElement indicator,
        double fromY,
        double fromHeight,
        double toY,
        double toHeight,
        TimeSpan duration)
    {
        StopStoryboard();
        Canvas.SetTop(indicator, fromY);
        indicator.Height = fromHeight;

        EasingFunctionBase easing = MotionTokens.EaseOut();
        var moveY = new DoubleAnimation
        {
            From = fromY,
            To = toY,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(moveY, indicator);
        Storyboard.SetTargetProperty(moveY, "(Canvas.Top)");

        var resizeHeight = new DoubleAnimation
        {
            From = fromHeight,
            To = toHeight,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(resizeHeight, indicator);
        Storyboard.SetTargetProperty(resizeHeight, "Height");

        _storyboard = new Storyboard { FillBehavior = FillBehavior.HoldEnd };
        _storyboard.Children.Add(moveY);
        _storyboard.Children.Add(resizeHeight);
        _storyboard.Completed += (_, _) =>
        {
            ApplyLayout(indicator, toY, toHeight);
        };
        _storyboard.Begin();
    }

    private void StopStoryboard()
    {
        _storyboard?.Stop();
        _storyboard = null;
    }
}
