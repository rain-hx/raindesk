using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace raindesk.Features.Launcher.Presentation.WinUI;

/// <summary>
/// 统一管理 Launcher 项目动画的替换、完成和容器回收清理。
/// 这是 WinUI 适配器，业务规则与集合状态仍保留在纯 Presentation/Application 类型中。
/// </summary>
internal sealed class LauncherItemAnimationCoordinator
{
    private readonly Dictionary<ListViewItem, Storyboard> _storyboards = new();
    private readonly Dictionary<ListViewItem, Point> _targetOffsets = new();

    public IReadOnlyCollection<ListViewItem> AnimatedItems => _storyboards.Keys;

    public bool Contains(ListViewItem item) => _storyboards.ContainsKey(item);

    public bool TryGetTargetOffset(ListViewItem item, out Point offset) =>
        _targetOffsets.TryGetValue(item, out offset);

    public void Slide(
        ListViewItem item,
        FrameworkElement root,
        double offsetX,
        double offsetY,
        TimeSpan duration,
        TimeSpan beginTime,
        EasingFunctionBase easing)
    {
        Stop(item);

        var transform = new TranslateTransform { X = offsetX, Y = offsetY };
        root.RenderTransform = transform;
        var slideX = Animation(offsetX, 0d, duration, beginTime, easing);
        var slideY = Animation(offsetY, 0d, duration, beginTime, easing);
        Storyboard.SetTarget(slideX, transform);
        Storyboard.SetTargetProperty(slideX, "X");
        Storyboard.SetTarget(slideY, transform);
        Storyboard.SetTargetProperty(slideY, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(slideX);
        storyboard.Children.Add(slideY);
        storyboard.Completed += (_, _) =>
        {
            if (!Release(item, storyboard)) return;
            ResetTransformIfCurrent(root, transform);
        };
        Start(item, storyboard);
    }

    public void FlyPlacement(
        ListViewItem item,
        FrameworkElement root,
        double offsetX,
        double offsetY,
        TimeSpan beginTime,
        EasingFunctionBase easing,
        TimeSpan duration,
        TimeSpan fadeDuration,
        double fallbackWidth,
        double fallbackHeight)
    {
        Stop(item);
        root.Opacity = 1d;

        var scale = new ScaleTransform
        {
            ScaleX = 0.84d,
            ScaleY = 0.84d,
            CenterX = (root.ActualWidth > 0d ? root.ActualWidth : fallbackWidth) / 2d,
            CenterY = (root.ActualHeight > 0d ? root.ActualHeight : fallbackHeight) / 2d
        };
        var translate = new TranslateTransform { X = offsetX, Y = offsetY };
        var transform = new TransformGroup();
        transform.Children.Add(scale);
        transform.Children.Add(translate);
        root.RenderTransform = transform;
        root.Opacity = 0d;

        var slideX = Animation(null, 0d, duration, beginTime, easing);
        var slideY = Animation(null, 0d, duration, beginTime, easing);
        var scaleX = Animation(null, 1d, duration, beginTime, easing);
        var scaleY = Animation(null, 1d, duration, beginTime, easing);
        var fade = Animation(null, 1d, fadeDuration, beginTime, easing);
        Storyboard.SetTarget(slideX, translate);
        Storyboard.SetTargetProperty(slideX, "X");
        Storyboard.SetTarget(slideY, translate);
        Storyboard.SetTargetProperty(slideY, "Y");
        Storyboard.SetTarget(scaleX, scale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        Storyboard.SetTarget(scaleY, scale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        Storyboard.SetTarget(fade, root);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(slideX);
        storyboard.Children.Add(slideY);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!Release(item, storyboard)) return;
            ResetTransformIfCurrent(root, transform);
            root.Opacity = 1d;
        };
        Start(item, storyboard);
    }

    public void PreviewOffset(
        ListViewItem item,
        FrameworkElement root,
        double offsetX,
        double offsetY,
        TimeSpan duration,
        TimeSpan beginTime,
        EasingFunctionBase easing)
    {
        double fromX = 0d;
        double fromY = 0d;
        if (root.RenderTransform is TranslateTransform existingTranslate)
        {
            fromX = existingTranslate.X;
            fromY = existingTranslate.Y;
        }

        StopTrackedStoryboard(item, resetVisual: false);
        var transform = new TranslateTransform { X = fromX, Y = fromY };
        root.RenderTransform = transform;
        var slideX = Animation(fromX, offsetX, duration, beginTime, easing);
        var slideY = Animation(fromY, offsetY, duration, beginTime, easing);
        Storyboard.SetTarget(slideX, transform);
        Storyboard.SetTargetProperty(slideX, "X");
        Storyboard.SetTarget(slideY, transform);
        Storyboard.SetTargetProperty(slideY, "Y");

        var storyboard = new Storyboard();
        storyboard.Children.Add(slideX);
        storyboard.Children.Add(slideY);
        storyboard.Completed += (_, _) =>
        {
            if (!Release(item, storyboard)) return;
            if (Math.Abs(offsetX) < 0.5d && Math.Abs(offsetY) < 0.5d)
            {
                ResetTransformIfCurrent(root, transform);
            }
        };
        Start(item, storyboard, new Point(offsetX, offsetY));
    }

    public void ScaleIn(
        ListViewItem item,
        FrameworkElement root,
        double initialScale,
        TimeSpan duration,
        EasingFunctionBase easing,
        double fallbackWidth,
        double fallbackHeight)
    {
        Stop(item);
        var scale = new ScaleTransform
        {
            ScaleX = initialScale,
            ScaleY = initialScale,
            CenterX = (root.ActualWidth > 0d ? root.ActualWidth : fallbackWidth) / 2d,
            CenterY = (root.ActualHeight > 0d ? root.ActualHeight : fallbackHeight) / 2d
        };
        root.RenderTransform = scale;

        var scaleX = Animation(null, 1d, duration, TimeSpan.Zero, easing);
        var scaleY = Animation(null, 1d, duration, TimeSpan.Zero, easing);
        Storyboard.SetTarget(scaleX, scale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        Storyboard.SetTarget(scaleY, scale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Completed += (_, _) =>
        {
            if (!Release(item, storyboard)) return;
            ResetTransformIfCurrent(root, scale);
        };
        Start(item, storyboard);
    }

    public void Stop(ListViewItem item) => StopTrackedStoryboard(item, resetVisual: true);

    public void StopAll()
    {
        foreach (ListViewItem item in _storyboards.Keys.ToArray())
        {
            Stop(item);
        }

        _storyboards.Clear();
        _targetOffsets.Clear();
    }

    private void Start(ListViewItem item, Storyboard storyboard, Point? targetOffset = null)
    {
        StopTrackedStoryboard(item, resetVisual: false);
        _storyboards[item] = storyboard;
        if (targetOffset.HasValue)
        {
            _targetOffsets[item] = targetOffset.Value;
        }
        else
        {
            _targetOffsets.Remove(item);
        }

        storyboard.Begin();
    }

    private bool Release(ListViewItem item, Storyboard storyboard)
    {
        if (!_storyboards.TryGetValue(item, out Storyboard? current)
            || !ReferenceEquals(current, storyboard))
        {
            return false;
        }

        _storyboards.Remove(item);
        _targetOffsets.Remove(item);
        return true;
    }

    private void StopTrackedStoryboard(ListViewItem item, bool resetVisual)
    {
        if (_storyboards.Remove(item, out Storyboard? storyboard))
        {
            try
            {
                storyboard.Stop();
            }
            catch
            {
                // 容器已回收或 XamlRoot 正在销毁。
            }
        }

        _targetOffsets.Remove(item);
        if (resetVisual && item.ContentTemplateRoot is FrameworkElement root)
        {
            root.RenderTransform = null;
            root.Opacity = 1d;
        }
    }

    private static DoubleAnimation Animation(
        double? from,
        double to,
        TimeSpan duration,
        TimeSpan beginTime,
        EasingFunctionBase easing) =>
        new()
        {
            From = from,
            To = to,
            Duration = duration,
            BeginTime = beginTime,
            EasingFunction = easing
        };

    private static void ResetTransformIfCurrent(FrameworkElement root, Transform transform)
    {
        try
        {
            if (ReferenceEquals(root.RenderTransform, transform))
            {
                root.RenderTransform = null;
            }
        }
        catch
        {
            // 快速缩放或关闭窗口时容器可能已经回收。
        }
    }
}
