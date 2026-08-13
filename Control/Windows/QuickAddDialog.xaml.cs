using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using raindesk.Services;
using raindesk.Services.Interfaces;
using raindesk.Util;
using raindesk.ViewModel;
using System;
using System.Threading.Tasks;

namespace raindesk.Control.Windows;

/// <summary>
/// 快速添加任务对话框（参考 TaskTrove 的 quick-add-dialog）。
///
/// 设计原则（与 TaskTrove 一致）：
/// - 只有标题是必填项
/// - 提供快捷按钮：今天、明天、日期选择、优先级
/// - 支持回车快速添加
/// - 添加后可选择保持打开（连续添加模式）
/// </summary>
public sealed partial class QuickAddDialog : ContentDialog
{
    private DateTime? _selectedDate;
    private TaskPriority? _selectedPriority;
    private bool _keepOpen;

    /// <summary>添加的任务</summary>
    public ToDoTask? CreatedTask { get; private set; }

    /// <summary>是否连续添加模式</summary>
    public bool KeepOpen
    {
        get => _keepOpen;
        set => _keepOpen = value;
    }

    public QuickAddDialog()
    {
        this.InitializeComponent();
    }

    // ── 生命周期 ──────────────────────────────────────────────────

    private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        TitleBox.Focus(FocusState.Programmatic);
    }

    // ── 添加 ──────────────────────────────────────────────────────

    private void OnAdd(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var title = TitleBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            args.Cancel = true;
            return;
        }

        var task = new ToDoTask
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Priority = _selectedPriority ?? TaskPriority.Medium,
            DueDate = _selectedDate,
            Status = TaskItemStatus.Pending,
            CreatedAt = DateTime.Now
        };

        // 如果选了日期但没选时间，默认设为当天结束
        if (task.DueDate.HasValue && string.IsNullOrEmpty(task.DueTime))
        {
            task.DueTime = "23:59";
        }

        var service = AppServices.TryGet<IToDoService>();
        service?.Add(task);

        CreatedTask = task;
        LogUtil.WriteLog($"快速添加任务: {task.Title}");

        // 连续添加模式：清空输入但不关闭
        if (_keepOpen)
        {
            args.Cancel = true;
            TitleBox.Text = string.Empty;
            _selectedDate = null;
            _selectedPriority = null;
            UpdateSelectedDateDisplay();
            UpdateSelectedPriorityDisplay();
            TitleBox.Focus(FocusState.Programmatic);
        }
    }

    private void OnCancel(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 什么都不做
    }

    // ── 标题输入 ──────────────────────────────────────────────────

    private void OnTitleKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            // 模拟点击"添加"按钮
            var title = TitleBox.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                // 直接调用添加逻辑
                var task = new ToDoTask
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = title,
                    Priority = _selectedPriority ?? TaskPriority.Medium,
                    DueDate = _selectedDate,
                    Status = TaskItemStatus.Pending,
                    CreatedAt = DateTime.Now
                };

                if (task.DueDate.HasValue && string.IsNullOrEmpty(task.DueTime))
                {
                    task.DueTime = "23:59";
                }

                var service = AppServices.TryGet<IToDoService>();
                service?.Add(task);
                CreatedTask = task;

                if (_keepOpen)
                {
                    TitleBox.Text = string.Empty;
                    _selectedDate = null;
                    _selectedPriority = null;
                    UpdateSelectedDateDisplay();
                    UpdateSelectedPriorityDisplay();
                }
                else
                {
                    this.Hide();
                }
            }
            e.Handled = true;
        }
    }

    // ── 日期选择 ──────────────────────────────────────────────────

    private void OnDateClick(object sender, RoutedEventArgs e)
    {
        DatePicker.Visibility = DatePicker.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today;
        UpdateSelectedDateDisplay();
        DatePicker.Visibility = Visibility.Collapsed;
    }

    private void OnTomorrowClick(object sender, RoutedEventArgs e)
    {
        _selectedDate = DateTime.Today.AddDays(1);
        UpdateSelectedDateDisplay();
        DatePicker.Visibility = Visibility.Collapsed;
    }

    private void OnDateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
        {
            _selectedDate = args.NewDate.Value.Date;
            UpdateSelectedDateDisplay();
        }
    }

    private void OnClearDate(object sender, RoutedEventArgs e)
    {
        _selectedDate = null;
        UpdateSelectedDateDisplay();
    }

    private void UpdateSelectedDateDisplay()
    {
        if (_selectedDate.HasValue)
        {
            SelectedDateRow.Visibility = Visibility.Visible;
            var date = _selectedDate.Value;
            var today = DateTime.Today;

            if (date == today)
                SelectedDateText.Text = "今天";
            else if (date == today.AddDays(1))
                SelectedDateText.Text = "明天";
            else if (date == today.AddDays(-1))
                SelectedDateText.Text = "昨天";
            else
                SelectedDateText.Text = date.ToString("MM/dd ddd");
        }
        else
        {
            SelectedDateRow.Visibility = Visibility.Collapsed;
        }
    }

    // ── 优先级选择 ────────────────────────────────────────────────

    private void OnPriorityClick(object sender, RoutedEventArgs e)
    {
        // 循环优先级: null → Critical → High → Medium → Low → null
        _selectedPriority = _selectedPriority switch
        {
            null => TaskPriority.Critical,
            TaskPriority.Critical => TaskPriority.High,
            TaskPriority.High => TaskPriority.Medium,
            TaskPriority.Medium => TaskPriority.Low,
            TaskPriority.Low => null,
            _ => null
        };
        UpdateSelectedPriorityDisplay();
    }

    private void OnClearPriority(object sender, RoutedEventArgs e)
    {
        _selectedPriority = null;
        UpdateSelectedPriorityDisplay();
    }

    private void UpdateSelectedPriorityDisplay()
    {
        if (_selectedPriority.HasValue)
        {
            SelectedPriorityRow.Visibility = Visibility.Visible;
            SelectedPriorityText.Text = _selectedPriority.Value switch
            {
                TaskPriority.Critical => "🔴 紧急",
                TaskPriority.High => "🟠 高",
                TaskPriority.Medium => "🔵 中",
                TaskPriority.Low => "⚪ 低",
                _ => ""
            };
        }
        else
        {
            SelectedPriorityRow.Visibility = Visibility.Collapsed;
        }
    }
}
