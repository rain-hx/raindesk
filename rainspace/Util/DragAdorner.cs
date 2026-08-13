using System;
using System.Numerics;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace rainspace.Util
{
    /// <summary>
    /// 拖拽预览宿主：在 Canvas 覆盖层上显示跟随鼠标的半透明预览图，
    /// 带 ThemeShadow 阴影和平滑曲线动画。
    /// </summary>
    public class DragPreviewHost
    {
        private Canvas? _overlayCanvas;
        private FrameworkElement? _previewElement;
        private ScaleTransform? _scaleTransform;
        private DispatcherQueueTimer? _followTimer;
        private double _pointerOffsetX;
        private double _pointerOffsetY;
        private double _elementWidth;
        private double _elementHeight;
        private double _currentX;
        private double _currentY;
        private double _targetX;
        private double _targetY;
        private bool _isActive;
        private bool _isPlacementPreviewActive;
        private Storyboard? _placementStoryboard;
        private const double DefaultFollowScale = 1.08d;

        public bool IsPlacementPreviewActive => _isPlacementPreviewActive;

        private static readonly EasingFunctionBase PopEase = new BackEase
        {
            EasingMode = EasingMode.EaseOut,
            Amplitude = 0.25
        };

        private static readonly EasingFunctionBase MoveEase = new CubicEase
        {
            EasingMode = EasingMode.EaseInOut
        };

        private static readonly EasingFunctionBase FadeEase = new CubicEase
        {
            EasingMode = EasingMode.EaseIn
        };

        /// <summary>
        /// 开始拖拽：使用已有图标位图，添加到覆盖层。
        /// </summary>
        public System.Threading.Tasks.Task BeginDragFromImageSource(
            ImageSource iconSource,
            Canvas overlayCanvas,
            double elementWidth,
            double elementHeight,
            double pointerOffsetX,
            double pointerOffsetY,
            double initialScale = 1.08,
            int dragCount = 1)
        {
            if (_isActive) EndDrag(false);

            _overlayCanvas = overlayCanvas;
            _pointerOffsetX = pointerOffsetX;
            _pointerOffsetY = pointerOffsetY;
            _elementWidth = elementWidth;
            _elementHeight = elementHeight;

            UIElement snapshot = BuildShortcutIconChromePreview(iconSource, _elementWidth, _elementHeight);

            return BeginDragWithSnapshot(snapshot, initialScale, dragCount);
        }

        /// <summary>
        /// 开始拖拽：截取源元素快照，添加到覆盖层。
        /// </summary>
        public async System.Threading.Tasks.Task BeginDrag(
            FrameworkElement sourceElement,
            Canvas overlayCanvas,
            double pointerOffsetX,
            double pointerOffsetY,
            double initialScale = 1.08,
            int dragCount = 1,
            double? elementWidth = null,
            double? elementHeight = null)
        {
            if (_isActive) EndDrag(false);

            _overlayCanvas = overlayCanvas;
            _pointerOffsetX = pointerOffsetX;
            _pointerOffsetY = pointerOffsetY;
            sourceElement.UpdateLayout();
            _elementWidth = elementWidth ?? (sourceElement.ActualWidth > 0d ? sourceElement.ActualWidth : sourceElement.Width);
            _elementHeight = elementHeight ?? (sourceElement.ActualHeight > 0d ? sourceElement.ActualHeight : sourceElement.Height);
            if (_elementWidth <= 0d) _elementWidth = 64d;
            if (_elementHeight <= 0d) _elementHeight = 64d;

            UIElement snapshot;
            try
            {
                var rtb = new RenderTargetBitmap();
                int renderWidth = Math.Max(1, (int)Math.Round(_elementWidth));
                int renderHeight = Math.Max(1, (int)Math.Round(_elementHeight));
                await rtb.RenderAsync(sourceElement, renderWidth, renderHeight);
                snapshot = new Image
                {
                    Source = rtb,
                    Width = _elementWidth,
                    Height = _elementHeight,
                    Stretch = Stretch.Uniform
                };
            }
            catch
            {
                snapshot = new Rectangle
                {
                    Width = _elementWidth,
                    Height = _elementHeight,
                    Fill = new SolidColorBrush(Colors.LightGray),
                    Opacity = 0.7
                };
            }

            await BeginDragWithSnapshot(snapshot, initialScale, dragCount);
        }

        private static UIElement BuildShortcutIconChromePreview(ImageSource iconSource, double width, double height)
        {
            double frameSize = Math.Min(48d, Math.Min(width, height));
            var host = new Grid
            {
                Width = width,
                Height = height
            };
            host.Children.Add(new Border
            {
                Width = width,
                Height = height,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 255, 255, 255))
            });
            host.Children.Add(new Border
            {
                Width = frameSize,
                Height = frameSize,
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Child = new Image
                {
                    Source = iconSource,
                    Width = frameSize,
                    Height = frameSize,
                    Stretch = Stretch.Uniform
                }
            });
            return host;
        }

        private System.Threading.Tasks.Task BeginDragWithSnapshot(
            UIElement snapshot,
            double initialScale,
            int dragCount)
        {

            UIElement previewContent = snapshot;
            if (dragCount > 1)
            {
                var contentGrid = new Grid
                {
                    Width = _elementWidth,
                    Height = _elementHeight
                };
                contentGrid.Children.Add(snapshot);

                var badge = new Border
                {
                    MinWidth = 20,
                    Height = 20,
                    Padding = new Thickness(6, 0, 6, 0),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, -4, -4, 0),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(255, 0, 103, 192)),
                    Child = new TextBlock
                    {
                        Text = dragCount.ToString(),
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Colors.White),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                contentGrid.Children.Add(badge);
                previewContent = contentGrid;
            }

            var border = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Colors.Transparent),
                Child = previewContent,
                Width = _elementWidth,
                Height = _elementHeight,
                Shadow = new ThemeShadow(),
                Opacity = 0.92,
                IsHitTestVisible = false
            };
            border.Translation = new Vector3(0, 0, 20);

            _scaleTransform = new ScaleTransform
            {
                CenterX = _elementWidth / 2,
                CenterY = _elementHeight / 2,
                ScaleX = 1.0,
                ScaleY = 1.0
            };
            border.RenderTransform = _scaleTransform;

            _previewElement = border;
            _overlayCanvas.Visibility = Visibility.Visible;
            _overlayCanvas.Children.Add(border);
            _isActive = true;

            AnimateScale(_scaleTransform, 1.0, initialScale, TimeSpan.FromMilliseconds(180), PopEase);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        /// <summary>
        /// 跨分组悬停预览时隐藏跟随鼠标的浮层，避免与网格内预览重复。
        /// </summary>
        public void SetSuppressed(bool suppressed)
        {
            if (_previewElement != null && !_isPlacementPreviewActive)
            {
                _previewElement.Opacity = suppressed ? 0d : 0.92d;
            }
        }

        /// <summary>
        /// 将拖拽预览吸附到目标槽位并缩小，模拟放入分组。
        /// </summary>
        public void AnimateToPlacement(double left, double top, double targetScale = 1.0d)
        {
            if (!_isActive || _previewElement == null || _scaleTransform == null) return;

            _isPlacementPreviewActive = true;
            StopFollowTimer();
            _placementStoryboard?.Stop();
            _previewElement.Opacity = 0.92d;

            _currentX = left;
            _currentY = top;
            _targetX = left;
            _targetY = top;
            Canvas.SetLeft(_previewElement, 0);
            Canvas.SetTop(_previewElement, 0);
            // WinUI3 不支持对 UIElement.Translation 的子属性做 Storyboard 动画。
            _previewElement.Translation = new Vector3((float)left, (float)top, 20);

            if (Math.Abs(_scaleTransform.ScaleX - targetScale) > 0.01d
                || Math.Abs(_scaleTransform.ScaleY - targetScale) > 0.01d)
            {
                AnimateScale(_scaleTransform, _scaleTransform.ScaleX, targetScale, TimeSpan.FromMilliseconds(220), MoveEase);
            }
        }

        /// <summary>
        /// 离开分组悬停预览，恢复跟随鼠标的放大浮层。
        /// </summary>
        public void ResumeFollowMode(double followScale = DefaultFollowScale)
        {
            if (!_isActive || _previewElement == null || _scaleTransform == null) return;
            if (!_isPlacementPreviewActive) return;

            _isPlacementPreviewActive = false;
            _placementStoryboard?.Stop();
            _previewElement.Opacity = 0.92d;

            AnimateScale(_scaleTransform, _scaleTransform.ScaleX, followScale, TimeSpan.FromMilliseconds(180), PopEase);
        }

        /// <summary>
        /// 退出槽位吸附并立即把浮层移到指针位置。
        /// </summary>
        public void ExitPlacementFollowAt(double windowX, double windowY, double followScale = DefaultFollowScale)
        {
            if (!_isActive || _previewElement == null || _scaleTransform == null) return;

            _isPlacementPreviewActive = false;
            _placementStoryboard?.Stop();
            _previewElement.Opacity = 0.92d;

            _targetX = windowX - _pointerOffsetX;
            _targetY = windowY - _pointerOffsetY;
            _currentX = _targetX;
            _currentY = _targetY;
            _previewElement.Translation = new Vector3((float)_currentX, (float)_currentY, 20);

            if (Math.Abs(_scaleTransform.ScaleX - followScale) > 0.01d)
            {
                AnimateScale(_scaleTransform, _scaleTransform.ScaleX, followScale, TimeSpan.FromMilliseconds(180), PopEase);
            }
        }

        /// <summary>
        /// 更新预览目标位置（窗口坐标），通过缓动曲线平滑跟随。
        /// </summary>
        public void UpdatePosition(double windowX, double windowY)
        {
            if (!_isActive || _previewElement == null || _overlayCanvas == null || _isPlacementPreviewActive) return;

            _targetX = windowX - _pointerOffsetX;
            _targetY = windowY - _pointerOffsetY;
            _currentX = _targetX;
            _currentY = _targetY;
            _previewElement.Translation = new Vector3((float)_currentX, (float)_currentY, 20);
            StopFollowTimer();
        }

        /// <summary>
        /// 结束拖拽：先吸附到目标格，再缩放淡出。
        /// </summary>
        public void EndDrag(bool animate = true, double? targetLeft = null, double? targetTop = null, Action? completed = null)
        {
            if (!_isActive) return;
            _isActive = false;
            StopFollowTimer();

            if (!animate || _previewElement == null)
            {
                RemoveOverlay();
                completed?.Invoke();
                return;
            }

            var sb = new Storyboard();
            bool hasTarget = targetLeft.HasValue && targetTop.HasValue;

            if (hasTarget)
            {
                _currentX = targetLeft!.Value;
                _currentY = targetTop!.Value;
                _targetX = _currentX;
                _targetY = _currentY;
                _previewElement.Translation = new Vector3((float)_currentX, (float)_currentY, 20);
            }

            if (_scaleTransform != null)
            {
                var scaleX = new DoubleAnimation
                {
                    From = _scaleTransform.ScaleX,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(hasTarget ? 220 : 160),
                    EasingFunction = MoveEase
                };
                var scaleY = new DoubleAnimation
                {
                    From = _scaleTransform.ScaleY,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(hasTarget ? 220 : 160),
                    EasingFunction = MoveEase
                };
                Storyboard.SetTarget(scaleX, _scaleTransform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");
                Storyboard.SetTarget(scaleY, _scaleTransform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");
                sb.Children.Add(scaleX);
                sb.Children.Add(scaleY);
            }

            var fadeOut = new DoubleAnimation
            {
                From = _previewElement.Opacity,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(160),
                BeginTime = TimeSpan.FromMilliseconds(hasTarget ? 140 : 80),
                EasingFunction = FadeEase
            };
            Storyboard.SetTarget(fadeOut, _previewElement);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");
            sb.Children.Add(fadeOut);

            sb.Completed += (_, _) =>
            {
                RemoveOverlay();
                completed?.Invoke();
            };
            sb.Begin();
        }

        private void EnsureFollowTimer()
        {
            if (_followTimer != null || _overlayCanvas == null) return;

            _followTimer = _overlayCanvas.DispatcherQueue.CreateTimer();
            _followTimer.Interval = TimeSpan.FromMilliseconds(16);
            _followTimer.Tick += FollowTimer_Tick;
            _followTimer.Start();
        }

        private void FollowTimer_Tick(DispatcherQueueTimer sender, object args)
        {
            if (_previewElement == null)
            {
                StopFollowTimer();
                return;
            }

            double dx = _targetX - _currentX;
            double dy = _targetY - _currentY;

            if (Math.Abs(dx) < 0.4d && Math.Abs(dy) < 0.4d)
            {
                _currentX = _targetX;
                _currentY = _targetY;
                StopFollowTimer();
            }
            else
            {
                const double smoothing = 0.22d;
                _currentX += dx * smoothing;
                _currentY += dy * smoothing;
            }

            _previewElement.Translation = new Vector3((float)_currentX, (float)_currentY, 20);
        }

        private void StopFollowTimer()
        {
            if (_followTimer == null) return;
            _followTimer.Stop();
            _followTimer.Tick -= FollowTimer_Tick;
            _followTimer = null;
        }

        private static void AnimateScale(
            ScaleTransform transform,
            double from,
            double to,
            TimeSpan duration,
            EasingFunctionBase easing)
        {
            var sb = new Storyboard();
            var scaleX = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = easing
            };
            var scaleY = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(scaleX, transform);
            Storyboard.SetTargetProperty(scaleX, "ScaleX");
            Storyboard.SetTarget(scaleY, transform);
            Storyboard.SetTargetProperty(scaleY, "ScaleY");
            sb.Children.Add(scaleX);
            sb.Children.Add(scaleY);
            sb.Begin();
        }

        private void RemoveOverlay()
        {
            StopFollowTimer();
            _placementStoryboard?.Stop();
            _placementStoryboard = null;
            _isPlacementPreviewActive = false;
            if (_overlayCanvas != null)
            {
                _overlayCanvas.Children.Clear();
                _overlayCanvas.Visibility = Visibility.Collapsed;
            }
            _previewElement = null;
            _scaleTransform = null;
            _overlayCanvas = null;
        }
    }
}
