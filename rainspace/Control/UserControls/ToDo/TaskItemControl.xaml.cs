using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using rainspace.Controls;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System;
using System.ComponentModel;

namespace rainspace.Control.UserControls.ToDo;

/// <summary>
/// 任务卡片控件（参考 TaskTrove 的 TaskItem）。
/// 显示单个任务的标题、优先级、截止日期、子任务进度等信息。
/// </summary>
public sealed partial class TaskItemControl : UserControl
{
    public static readonly DependencyProperty TaskProperty =
        DependencyProperty.Register(
            nameof(Task),
            typeof(ToDoTask),
            typeof(TaskItemControl),
            new PropertyMetadata(null, OnTaskChanged));

    private static readonly SolidColorBrush SelectionBrush = new(Microsoft.UI.ColorHelper.FromArgb(22, 8, 123, 255));
    private static readonly SolidColorBrush CompletedTextBrush = new(Microsoft.UI.ColorHelper.FromArgb(255, 86, 101, 140));
    private static readonly SolidColorBrush CriticalBrush = new(Microsoft.UI.ColorHelper.FromArgb(255, 239, 68, 68));
    private static readonly SolidColorBrush HighBrush = new(Microsoft.UI.ColorHelper.FromArgb(255, 245, 158, 11));
    private static readonly SolidColorBrush MediumBrush = new(Microsoft.UI.ColorHelper.FromArgb(255, 8, 123, 255));
    private static readonly SolidColorBrush LowBrush = new(Microsoft.UI.ColorHelper.FromArgb(255, 86, 101, 140));

    private ToDoTask? _task;
    private bool _isSelected;
    private bool _isSelectionMode;

    public TaskItemControl()
    {
        this.InitializeComponent();
    }

    // ── 属性 ──────────────────────────────────────────────────────

    public ToDoTask? Task
    {
        get => (ToDoTask?)GetValue(TaskProperty);
        set => SetValue(TaskProperty, value);
    }

    /// <summary>是否被选中</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            SelectionCheckBox.IsChecked = value;
            UpdateSelectionVisual();
        }
    }

    /// <summary>任务点击时触发（用于打开编辑器）</summary>
    public event Action<ToDoTask>? TaskClicked;

    /// <summary>选中状态变化事件</summary>
    public event Action<TaskItemControl, bool>? SelectionChanged;

    // ── 事件处理 ──────────────────────────────────────────────────

    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        _isSelected = SelectionCheckBox.IsChecked == true;
        UpdateSelectionVisual();
        SelectionChanged?.Invoke(this, _isSelected);
    }

    private void OnToggleComplete(object sender, RoutedEventArgs e)
    {
        if (_task == null) return;

        var service = AppServices.TryGet<IToDoService>();
        service?.ToggleComplete(_task.Id);
        UpdateDisplay();
    }

    private void OnInteractiveChildTapped(object sender, TappedRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnInteractiveChildPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_task == null) return;

        var service = AppServices.TryGet<IToDoService>();
        service?.Delete(_task.Id);
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (IsFromInteractiveChild(e.OriginalSource))
        {
            return;
        }

        DeleteButton.Visibility = Visibility.Visible;
        CardBorder.Background = _isSelected
            ? SelectionBrush
            : GetResourceBrush("CardHoverBrush", SelectionBrush);

        if (MotionPreferences.ShouldAnimate)
        {
            ((Storyboard)Resources["HoverOutStoryboard"]).Stop();
            ((Storyboard)Resources["HoverInStoryboard"]).Begin();
        }
        else
        {
            CardScale.ScaleX = 1.008;
            CardScale.ScaleY = 1.008;
        }
    }

    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        DeleteButton.Visibility = Visibility.Collapsed;
        UpdateSelectionVisual();

        if (MotionPreferences.ShouldAnimate)
        {
            ((Storyboard)Resources["HoverInStoryboard"]).Stop();
            ((Storyboard)Resources["HoverOutStoryboard"]).Begin();
        }
        else
        {
            CardScale.ScaleX = 1;
            CardScale.ScaleY = 1;
        }
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        if (IsFromInteractiveChild(e.OriginalSource))
        {
            e.Handled = true;
            return;
        }

        if (_isSelectionMode)
        {
            IsSelected = !_isSelected;
            SelectionChanged?.Invoke(this, _isSelected);
            e.Handled = true;
        }
        else if (_task != null)
        {
            TaskClicked?.Invoke(_task);
            e.Handled = true;
        }
    }

    private bool IsFromInteractiveChild(object? source)
    {
        if (source is not DependencyObject current)
        {
            return false;
        }

        while (current != null && current != this)
        {
            if (current == CompleteCheckBox || current == SelectionCheckBox || current == DeleteButton)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    // ── 内联编辑 ──────────────────────────────────────────────────

    private void OnTitleDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (_task == null) return;

        // 切换到编辑模式
        TitleText.Visibility = Visibility.Collapsed;
        TitleEditBox.Visibility = Visibility.Visible;
        TitleEditBox.Text = _task.Title;
        TitleEditBox.Focus(FocusState.Programmatic);
        TitleEditBox.SelectAll();
    }

    private void OnTitleEditKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            CommitTitleEdit();
            e.Handled = true;
        }
        else if (e.Key == global::Windows.System.VirtualKey.Escape)
        {
            CancelTitleEdit();
            e.Handled = true;
        }
    }

    private void OnTitleEditLostFocus(object sender, RoutedEventArgs e)
    {
        CommitTitleEdit();
    }

    private void CommitTitleEdit()
    {
        if (_task == null) return;

        var newTitle = TitleEditBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(newTitle) && newTitle != _task.Title)
        {
            _task.Title = newTitle;
            var service = AppServices.TryGet<IToDoService>();
            service?.Update(_task);
        }

        // 切换回显示模式
        TitleEditBox.Visibility = Visibility.Collapsed;
        TitleText.Visibility = Visibility.Visible;
        UpdateDisplay();
    }

    private void CancelTitleEdit()
    {
        TitleEditBox.Visibility = Visibility.Collapsed;
        TitleText.Visibility = Visibility.Visible;
    }

    // ── 选择模式 ──────────────────────────────────────────────────

    /// <summary>显示/隐藏多选复选框</summary>
    public void SetSelectionMode(bool enabled)
    {
        _isSelectionMode = enabled;
        SelectionCheckBox.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSelectionVisual()
    {
        CardBorder.Background = _isSelected
            ? SelectionBrush
            : GetResourceBrush("ControlFillBrush", SelectionBrush);
    }

    // ── 更新显示 ──────────────────────────────────────────────────

    private void UpdateDisplay()
    {
        if (_task == null) return;

        // 标题
        TitleText.Text = _task.Title;
        TitleText.Foreground = _task.IsCompleted
            ? GetResourceBrush("SecondaryTextBrush", CompletedTextBrush)
            : GetResourceBrush("AppForegroundBrush", MediumBrush);

        // 复选框
        CompleteCheckBox.IsChecked = _task.IsCompleted;

        // 优先级色条（过期时统一显示红色）
        PriorityBar.Background = _task.IsOverdue
            ? CriticalBrush
            : _task.Priority switch
            {
                TaskPriority.Critical => CriticalBrush,
                TaskPriority.High => HighBrush,
                TaskPriority.Medium => MediumBrush,
                TaskPriority.Low => LowBrush,
                _ => MediumBrush
            };

        // 过期标签（标题下方）
        if (_task.IsOverdue && _task.DueDate.HasValue)
        {
            var overdueDays = (DateTime.Today - _task.DueDate.Value.Date).Days;
            OverdueBadge.Visibility = Visibility.Visible;
            OverdueBadge.Background = GetResourceBrush("AccentLightBrush", SelectionBrush);
            OverdueText.Text = $"已过期 {overdueDays} 天";
            OverdueText.Foreground = GetResourceBrush("rainspaceRedBrush", CriticalBrush);
        }
        else
        {
            OverdueBadge.Visibility = Visibility.Collapsed;
        }

        // 右侧日期
        DateRightText.Text = GetRightDateDisplay(_task);
        var secondaryTextBrush = GetResourceBrush("SecondaryTextBrush", LowBrush);
        DateRightText.Foreground = secondaryTextBrush;
        DateIcon.Foreground = secondaryTextBrush;

        // 元数据行
        bool hasMetadata = false;

        // 子任务进度
        if (_task.Subtasks.Count > 0)
        {
            SubtaskText.Text = _task.SubtaskProgress;
            SubtaskText.Visibility = Visibility.Visible;
            hasMetadata = true;
        }
        else
        {
            SubtaskText.Visibility = Visibility.Collapsed;
        }

        // 标签
        if (_task.Tags.Count > 0)
        {
            TagsText.Text = _task.TagsDisplay;
            TagsText.Visibility = Visibility.Visible;
            hasMetadata = true;
        }
        else
        {
            TagsText.Visibility = Visibility.Collapsed;
        }

        // 循环标记
        RecurringIcon.Visibility = _task.IsRecurring ? Visibility.Visible : Visibility.Collapsed;
        if (_task.IsRecurring) hasMetadata = true;

        MetadataRow.Visibility = hasMetadata ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetRightDateDisplay(ToDoTask task)
    {
        if (!task.DueDate.HasValue)
        {
            return "未设置日期";
        }

        var date = task.DueDate.Value.Date;
        var today = DateTime.Today;

        if (date == today)
        {
            return "今天";
        }

        if (date == today.AddDays(1))
        {
            return "明天";
        }

        return date.ToString("yyyy/MM/dd");
    }

    private static Brush GetResourceBrush(string key, Brush fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush
            ? brush
            : fallback;
    }

    private static void OnTaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TaskItemControl control)
        {
            return;
        }

        control.AttachTask(e.NewValue as ToDoTask);
        control.UpdateDisplay();
    }

    private void AttachTask(ToDoTask? task)
    {
        if (_task != null)
        {
            _task.PropertyChanged -= OnTaskPropertyChanged;
        }

        _task = task;

        if (_task != null)
        {
            _task.PropertyChanged += OnTaskPropertyChanged;
        }
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null
            or nameof(ToDoTask.DueDate)
            or nameof(ToDoTask.DueDateDisplay)
            or nameof(ToDoTask.IsOverdue)
            or nameof(ToDoTask.IsCompleted)
            or nameof(ToDoTask.Title)
            or nameof(ToDoTask.Priority)
            or nameof(ToDoTask.SubtaskProgress)
            or nameof(ToDoTask.TagsDisplay)
            or nameof(ToDoTask.IsRecurring))
        {
            UpdateDisplay();
        }
    }
}
