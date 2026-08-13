using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace rainspace.Control.Windows;

/// <summary>
/// 任务编辑对话框（参考 TaskTrove 的 TaskItem 编辑模式）。
/// 替代旧的 ToDoInfoWindow，支持优先级、标签、子任务、循环配置。
/// </summary>
public sealed partial class TaskEditDialog : UserControl
{
    private readonly ToDoTask? _existingTask;
    private readonly List<Subtask> _subtasks = new();
    private readonly List<string> _tags = new();
    private bool _updatingDateCombos;
    private bool _isInitialized;

    public event EventHandler? CloseRequested;

    /// <summary>创建新任务</summary>
    public TaskEditDialog()
    {
        this.InitializeComponent();
        DialogTitleText.Text = "新建任务";
        RecurringCombo.SelectedIndex = 0;
        InitializeDateCombos(DateTime.Today.AddDays(1));
        _isInitialized = true;
    }

    /// <summary>编辑现有任务</summary>
    public TaskEditDialog(ToDoTask task) : this()
    {
        _existingTask = task;
        DialogTitleText.Text = "编辑任务";
        PopulateFromTask(task);
    }

    /// <summary>保存后的任务</summary>
    public ToDoTask? Result { get; private set; }

    // ── 填充表单 ──────────────────────────────────────────────────

    private void PopulateFromTask(ToDoTask task)
    {
        TitleBox.Text = task.Title;
        DescriptionBox.Text = task.Description;

        // 优先级
        switch (task.Priority)
        {
            case TaskPriority.Critical: PriorityCombo.SelectedIndex = 0; break;
            case TaskPriority.High: PriorityCombo.SelectedIndex = 1; break;
            case TaskPriority.Medium: PriorityCombo.SelectedIndex = 2; break;
            case TaskPriority.Low: PriorityCombo.SelectedIndex = 3; break;
        }

        // 截止日期
        if (task.DueDate.HasValue)
        {
            SelectDate(task.DueDate.Value.Date);
        }

        // 循环
        if (task.Recurring.IsRecurring)
        {
            if (task.Recurring.Expression == "0 0 * * *") RecurringCombo.SelectedIndex = 1;
            else if (task.Recurring.Expression == "0 0 * * 0") RecurringCombo.SelectedIndex = 2;
            else if (task.Recurring.Expression == "0 0 1 * *") RecurringCombo.SelectedIndex = 3;
            else
            {
                RecurringCombo.SelectedIndex = 4;
                CronExpressionBox.Text = task.Recurring.Expression;
                CronExpressionBox.Visibility = Visibility.Visible;
            }
        }

        // 子任务
        foreach (var sub in task.Subtasks)
        {
            _subtasks.Add(sub.Clone());
        }
        RefreshSubtaskList();

        // 标签
        _tags.AddRange(task.Tags);
        RefreshTagList();

        // 完成时间
        if (task.CompletedAt.HasValue)
        {
            CompletedAtRow.Visibility = Visibility.Visible;
            CompletedAtText.Text = task.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    // ── 保存 ──────────────────────────────────────────────────────

    private bool TrySaveTask()
    {
        // 验证标题
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            TitleBox.PlaceholderText = "标题不能为空！";
            TitleBox.Focus(FocusState.Programmatic);
            return false;
        }

        var task = _existingTask ?? new ToDoTask();
        task.Title = TitleBox.Text.Trim();
        task.Description = string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim();

        // 优先级
        task.Priority = GetSelectedPriority();

        // 截止日期
        task.DueDate = GetSelectedDueDate();

        // 循环
        task.Recurring = BuildRecurringConfig();

        // 子任务
        task.Subtasks.Clear();
        foreach (var sub in _subtasks)
        {
            task.Subtasks.Add(sub);
        }

        // 标签
        task.Tags = new List<string>(_tags);

        // 保存到服务
        var service = AppServices.TryGet<IToDoService>();
        if (service != null)
        {
            if (_existingTask != null)
            {
                service.Update(task);
            }
            else
            {
                service.Add(task);
            }
        }

        Result = task;
        return true;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (TrySaveTask())
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // ── 优先级 ────────────────────────────────────────────────────

    private TaskPriority GetSelectedPriority()
    {
        if (PriorityCombo.SelectedItem is ComboBoxItem item
            && int.TryParse(item.Tag?.ToString(), out int priority))
        {
            return priority switch
            {
                1 => TaskPriority.Critical,
                2 => TaskPriority.High,
                4 => TaskPriority.Low,
                _ => TaskPriority.Medium
            };
        }

        return TaskPriority.Medium;
    }

    // ── 截止日期 ──────────────────────────────────────────────────

    private void InitializeDateCombos(DateTime selectedDate)
    {
        _updatingDateCombos = true;

        YearCombo.Items.Clear();
        MonthCombo.Items.Clear();
        DayCombo.Items.Clear();

        int currentYear = DateTime.Today.Year;
        for (int year = currentYear - 1; year <= currentYear + 8; year++)
        {
            YearCombo.Items.Add(new ComboBoxItem { Content = $"{year}年", Tag = year });
        }

        for (int month = 1; month <= 12; month++)
        {
            MonthCombo.Items.Add(new ComboBoxItem { Content = $"{month}月", Tag = month });
        }

        _updatingDateCombos = false;
        SelectDate(selectedDate);
    }

    private void SelectDate(DateTime date)
    {
        _updatingDateCombos = true;

        SelectComboItemByTag(YearCombo, date.Year);
        SelectComboItemByTag(MonthCombo, date.Month);
        RebuildDayCombo(date.Year, date.Month, date.Day);

        _updatingDateCombos = false;
    }

    private void OnDateComboChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingDateCombos)
        {
            return;
        }

        if (GetSelectedTag(YearCombo) is not int year || GetSelectedTag(MonthCombo) is not int month)
        {
            return;
        }

        int selectedDay = GetSelectedTag(DayCombo) as int? ?? 1;

        if (ReferenceEquals(sender, DayCombo))
        {
            return;
        }

        RebuildDayCombo(year, month, selectedDay);
    }

    private void RebuildDayCombo(int year, int month, int preferredDay)
    {
        _updatingDateCombos = true;

        DayCombo.Items.Clear();
        int days = DateTime.DaysInMonth(year, month);
        int selectedDay = Math.Clamp(preferredDay, 1, days);
        for (int day = 1; day <= days; day++)
        {
            DayCombo.Items.Add(new ComboBoxItem { Content = $"{day}日", Tag = day });
        }

        SelectComboItemByTag(DayCombo, selectedDay);
        _updatingDateCombos = false;
    }

    private DateTime? GetSelectedDueDate()
    {
        if (GetSelectedTag(YearCombo) is int year
            && GetSelectedTag(MonthCombo) is int month
            && GetSelectedTag(DayCombo) is int day)
        {
            return new DateTime(year, month, day);
        }

        return null;
    }

    private static object? GetSelectedTag(ComboBox comboBox)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Tag;
    }

    private static void SelectComboItemByTag(ComboBox comboBox, int tag)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item
                && int.TryParse(item.Tag?.ToString(), out int itemTag)
                && itemTag == tag)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }

        comboBox.SelectedIndex = -1;
    }

    // ── 循环设置 ──────────────────────────────────────────────────

    private void OnRecurringChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || CronExpressionBox is null || CronValidationText is null)
        {
            return;
        }

        var combo = sender as ComboBox;
        var item = combo?.SelectedItem as ComboBoxItem;
        var tag = item?.Tag?.ToString();

        CronExpressionBox.Visibility = tag == "Custom" ? Visibility.Visible : Visibility.Collapsed;
        CronValidationText.Visibility = Visibility.Collapsed;
    }

    private RecurringConfig BuildRecurringConfig()
    {
        var combo = RecurringCombo.SelectedItem as ComboBoxItem;
        var tag = combo?.Tag?.ToString();

        return tag switch
        {
            "Daily" => new RecurringConfig { Type = RecurringType.Cron, Expression = "0 0 * * *" },
            "Weekly" => new RecurringConfig { Type = RecurringType.Cron, Expression = "0 0 * * 0" },
            "Monthly" => new RecurringConfig { Type = RecurringType.Cron, Expression = "0 0 1 * *" },
            "Custom" => new RecurringConfig
            {
                Type = RecurringType.Cron,
                Expression = CronExpressionBox.Text?.Trim()
            },
            _ => new RecurringConfig()
        };
    }

    // ── 子任务 ────────────────────────────────────────────────────

    private void OnAddSubtask(object sender, RoutedEventArgs e)
    {
        string title = string.IsNullOrWhiteSpace(SubtaskInput.Text)
            ? "新子任务"
            : SubtaskInput.Text.Trim();
        var subtask = new Subtask
        {
            Title = title,
            SortOrder = _subtasks.Count
        };
        _subtasks.Add(subtask);
        SubtaskInput.Text = string.Empty;
        RefreshSubtaskList();
    }

    private void RefreshSubtaskList()
    {
        SubtaskList.Items.Clear();
        var fieldStyle = Resources["TaskFieldTextBoxStyle"] as Style;

        for (int i = 0; i < _subtasks.Count; i++)
        {
            var sub = _subtasks[i];
            var grid = new Grid
            {
                ColumnSpacing = 8,
                Margin = new Thickness(0, 4, 0, 0)
            };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var checkbox = new CheckBox
            {
                IsChecked = sub.IsCompleted,
                Tag = i,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkbox.Checked += (_, _) => { if (checkbox.Tag is int idx) _subtasks[idx].IsCompleted = true; };
            checkbox.Unchecked += (_, _) => { if (checkbox.Tag is int idx) _subtasks[idx].IsCompleted = false; };
            Grid.SetColumn(checkbox, 0);

            var textBox = new TextBox
            {
                Text = sub.Title,
                Tag = i,
                Style = fieldStyle,
                MinHeight = 34,
                Height = 34,
                Padding = new Thickness(12, 4, 12, 4),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            textBox.TextChanged += (_, _) => { if (textBox.Tag is int idx) _subtasks[idx].Title = textBox.Text; };
            Grid.SetColumn(textBox, 1);

            var deleteBtn = new Button
            {
                Content = "×",
                Tag = i,
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(8),
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 86, 101, 140)),
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (_, _) => { if (deleteBtn.Tag is int idx) { _subtasks.RemoveAt(idx); RefreshSubtaskList(); } };
            Grid.SetColumn(deleteBtn, 2);

            grid.Children.Add(checkbox);
            grid.Children.Add(textBox);
            grid.Children.Add(deleteBtn);

            SubtaskList.Items.Add(grid);
        }
    }

    // ── 标签 ──────────────────────────────────────────────────────

    private void OnAddTag(object sender, RoutedEventArgs e)
    {
        AddTagFromInput();
    }

    private void OnTagKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            AddTagFromInput();
            e.Handled = true;
        }
    }

    private void AddTagFromInput()
    {
        var tag = TagInput.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(tag) && !_tags.Contains(tag))
        {
            _tags.Add(tag);
            RefreshTagList();
        }
        TagInput.Text = string.Empty;
    }

    private void RefreshTagList()
    {
        TagList.Items.Clear();
        foreach (var tag in _tags)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
            stack.Children.Add(new TextBlock { Text = tag, VerticalAlignment = VerticalAlignment.Center });
            var removeBtn = new Button { Content = "×", Tag = tag, Padding = new Thickness(4), Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent) };
            removeBtn.Click += (_, _) => { _tags.Remove((string)tag); RefreshTagList(); };
            stack.Children.Add(removeBtn);
            TagList.Items.Add(stack);
        }
    }
}
