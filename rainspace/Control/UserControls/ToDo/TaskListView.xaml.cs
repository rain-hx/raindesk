using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace rainspace.Control.UserControls.ToDo;

/// <summary>
/// 任务列表视图（参考 TaskTrove 的 filtered tasks）。
/// 集成筛选、排序、搜索功能。
/// </summary>
public sealed partial class TaskListView : UserControl
{
    private readonly ToDoFilter _filter = new();
    private IToDoService? _todoService;
    private TaskDragDropHelper? _dragDropHelper;
    private readonly HashSet<string> _selectedTaskIds = new();
    private bool _isSelectionMode;
    private bool _isLoaded;
    private bool _suppressFilterRefresh;
    private bool _suppressServiceRefresh;
    private DispatcherQueueTimer? _searchDebounceTimer;
    private const int SearchDebounceMs = 180;

    public TaskListView()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;

        // 监听筛选条件变化
        _filter.PropertyChanged += (_, _) =>
        {
            if (!_suppressFilterRefresh)
            {
                RefreshList();
            }
        };
    }

    /// <summary>筛选器（供外部访问）</summary>
    public ToDoFilter Filter => _filter;

    /// <summary>当前显示的任务集合，交给 ListView 复用容器和过渡动画。</summary>
    public ObservableCollection<ToDoTask> VisibleTasks { get; } = new();

    /// <summary>任务点击事件</summary>
    public event Action<ToDoTask>? TaskClicked;

    /// <summary>列表刷新完成事件</summary>
    public event Action<int>? ListRefreshed;

    // ── 生命周期 ──────────────────────────────────────────────────

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded)
        {
            RefreshList();
            return;
        }

        _isLoaded = true;
        _todoService = AppServices.TryGet<IToDoService>();
        if (_todoService != null)
        {
            _todoService.TaskAdded += OnTaskAdded;
            _todoService.TaskUpdated += OnTaskUpdated;
            _todoService.TaskDeleted += OnTaskDeleted;

            _dragDropHelper = new TaskDragDropHelper(TaskList, _todoService);
            _dragDropHelper.TaskReordered += OnTaskReordered;
        }
        FilterBar.Filter = _filter;
        RefreshList();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded)
        {
            return;
        }

        _isLoaded = false;

        if (_todoService != null)
        {
            _todoService.TaskAdded -= OnTaskAdded;
            _todoService.TaskUpdated -= OnTaskUpdated;
            _todoService.TaskDeleted -= OnTaskDeleted;
        }

        if (_dragDropHelper != null)
        {
            _dragDropHelper.TaskReordered -= OnTaskReordered;
            _dragDropHelper.Dispose();
            _dragDropHelper = null;
        }

        _searchDebounceTimer?.Stop();
    }

    private void OnTaskAdded(ToDoTask task) => QueueServiceRefresh();

    private void OnTaskUpdated(ToDoTask task)
    {
        if (TryRefreshTaskInPlace(task))
        {
            return;
        }

        QueueServiceRefresh();
    }

    private void OnTaskDeleted(string id) => QueueServiceRefresh();

    private void OnTaskReordered() => DispatcherQueue.TryEnqueue(RefreshList);

    // ── 公共方法 ──────────────────────────────────────────────────

    /// <summary>刷新列表</summary>
    public void RefreshList()
    {
        if (_todoService == null) return;

        var allTasks = _todoService.GetAll();
        var filtered = _filter.Apply(allTasks);

        UpdateVisibleTasks(filtered);
        RefreshVisibleSelectionState();

        // 更新统计
        UpdateStats(allTasks);

        // 空状态
        EmptyState.Visibility = VisibleTasks.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        TaskList.Visibility = VisibleTasks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

        ListRefreshed?.Invoke(VisibleTasks.Count);
    }

    /// <summary>切换筛选状态</summary>
    public void SetStatusFilter(ToDoStatusFilter filter)
    {
        _filter.StatusFilter = filter;
    }

    // ── 事件处理 ──────────────────────────────────────────────────

    private void OnSearchTextChanged(object sender, TextChangedEventArgs args)
    {
        if (sender is TextBox textBox)
        {
            _suppressFilterRefresh = true;
            try
            {
                _filter.SearchQuery = textBox.Text;
            }
            finally
            {
                _suppressFilterRefresh = false;
            }

            ScheduleSearchRefresh();
        }
    }

    private void OnTaskItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ToDoTask task)
        {
            if (_isSelectionMode)
            {
                ToggleTaskSelection(task.Id);
            }
            else
            {
                TaskClicked?.Invoke(task);
            }
        }
    }

    // ── 批量操作 ──────────────────────────────────────────────────

    /// <summary>进入选择模式</summary>
    public void EnterSelectionMode()
    {
        _isSelectionMode = true;
        _selectedTaskIds.Clear();
        BatchToolbar.Visibility = Visibility.Visible;
        TaskList.SelectionMode = ListViewSelectionMode.Multiple;
        RefreshVisibleSelectionState();
    }

    /// <summary>退出选择模式</summary>
    public void ExitSelectionMode()
    {
        _isSelectionMode = false;
        BatchToolbar.Visibility = Visibility.Collapsed;
        _selectedTaskIds.Clear();
        TaskList.SelectedItems.Clear();
        TaskList.SelectionMode = ListViewSelectionMode.None;
        RefreshVisibleSelectionState();
    }

    private void OnItemSelectionChanged(TaskItemControl item, bool isSelected)
    {
        if (item.Task == null)
        {
            return;
        }

        if (isSelected)
        {
            _selectedTaskIds.Add(item.Task.Id);
            if (!TaskList.SelectedItems.Contains(item.Task))
            {
                TaskList.SelectedItems.Add(item.Task);
            }
        }
        else
        {
            _selectedTaskIds.Remove(item.Task.Id);
            TaskList.SelectedItems.Remove(item.Task);
        }
    }

    private void OnSelectAll(object sender, RoutedEventArgs e)
    {
        _selectedTaskIds.Clear();
        TaskList.SelectedItems.Clear();
        foreach (var task in VisibleTasks)
        {
            _selectedTaskIds.Add(task.Id);
            TaskList.SelectedItems.Add(task);
        }

        RefreshVisibleSelectionState();
    }

    private void OnCompleteSelected(object sender, RoutedEventArgs e)
    {
        if (_todoService == null) return;

        RunBatchOperation(() =>
        {
            foreach (var id in _selectedTaskIds.ToList())
            {
                _todoService.ToggleComplete(id);
            }
        });

        ExitSelectionMode();
        RefreshList();
    }

    private void OnDeleteSelected(object sender, RoutedEventArgs e)
    {
        if (_todoService == null) return;

        RunBatchOperation(() =>
        {
            foreach (var id in _selectedTaskIds.ToList())
            {
                _todoService.Delete(id);
            }
        });

        ExitSelectionMode();
        RefreshList();
    }

    private void OnSetPrioritySelected(object sender, RoutedEventArgs e)
    {
        // TODO: 显示优先级选择对话框
    }

    private void OnMoveToProjectSelected(object sender, RoutedEventArgs e)
    {
        // TODO: 显示项目选择对话框
    }

    private void OnCancelSelection(object sender, RoutedEventArgs e)
    {
        ExitSelectionMode();
    }

    // ── 辅助方法 ──────────────────────────────────────────────────

    private void UpdateStats(IReadOnlyList<ToDoTask> allTasks)
    {
        int active = allTasks.Count(t => t.IsActive);
        int completed = allTasks.Count(t => t.IsCompleted);
        int overdue = allTasks.Count(t => t.IsOverdue);

        ActiveCount.Text = $"待办： {active}";
        CompletedCount.Text = $"已完成： {completed}";
        OverdueCount.Text = $"已过期： {overdue}";
    }

    private void UpdateVisibleTasks(IReadOnlyList<ToDoTask> filtered)
    {
        var filteredIds = filtered.Select(t => t.Id).ToHashSet();

        for (int i = VisibleTasks.Count - 1; i >= 0; i--)
        {
            if (!filteredIds.Contains(VisibleTasks[i].Id))
            {
                VisibleTasks.RemoveAt(i);
            }
        }

        var visibleIndices = BuildVisibleTaskIndex();
        for (int i = 0; i < filtered.Count; i++)
        {
            var task = filtered[i];
            if (!visibleIndices.TryGetValue(task.Id, out int existingIndex))
            {
                VisibleTasks.Insert(i, task);
                ReindexVisibleTasks(visibleIndices, i);
            }
            else if (existingIndex != i)
            {
                VisibleTasks.Move(existingIndex, i);
                ReindexVisibleTasks(visibleIndices, Math.Min(existingIndex, i));
            }
        }
    }

    /// <summary>
    /// 任务属性变更但仍在列表同一位置时，避免整表刷新触发动画。
    /// </summary>
    private bool TryRefreshTaskInPlace(ToDoTask task)
    {
        if (_todoService == null)
        {
            return false;
        }

        var allTasks = _todoService.GetAll();
        var filtered = _filter.Apply(allTasks);
        int newIndex = filtered.FindIndex(t => t.Id == task.Id);
        if (newIndex < 0)
        {
            return false;
        }

        int existingIndex = -1;
        for (int i = 0; i < VisibleTasks.Count; i++)
        {
            if (VisibleTasks[i].Id == task.Id)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex < 0)
        {
            return false;
        }

        if (existingIndex == newIndex)
        {
            if (!ReferenceEquals(VisibleTasks[existingIndex], task))
            {
                VisibleTasks[existingIndex] = task;
            }

            UpdateStats(allTasks);
            ListRefreshed?.Invoke(VisibleTasks.Count);
            return true;
        }

        return false;
    }

    private void ScheduleSearchRefresh()
    {
        if (_searchDebounceTimer == null)
        {
            _searchDebounceTimer = DispatcherQueue.CreateTimer();
            _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(SearchDebounceMs);
            _searchDebounceTimer.Tick += (_, _) =>
            {
                _searchDebounceTimer.Stop();
                RefreshList();
            };
        }

        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void QueueServiceRefresh()
    {
        if (_suppressServiceRefresh)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(RefreshList);
    }

    private void RunBatchOperation(Action operation)
    {
        _suppressServiceRefresh = true;
        try
        {
            operation();
        }
        finally
        {
            _suppressServiceRefresh = false;
        }
    }

    private Dictionary<string, int> BuildVisibleTaskIndex()
    {
        var indices = new Dictionary<string, int>();
        for (int i = 0; i < VisibleTasks.Count; i++)
        {
            indices[VisibleTasks[i].Id] = i;
        }

        return indices;
    }

    private void ReindexVisibleTasks(Dictionary<string, int> indices, int startIndex)
    {
        for (int i = Math.Max(0, startIndex); i < VisibleTasks.Count; i++)
        {
            indices[VisibleTasks[i].Id] = i;
        }
    }

    private void ToggleTaskSelection(string taskId)
    {
        var task = VisibleTasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
        {
            return;
        }

        if (_selectedTaskIds.Remove(taskId))
        {
            TaskList.SelectedItems.Remove(task);
        }
        else
        {
            _selectedTaskIds.Add(taskId);
            TaskList.SelectedItems.Add(task);
        }

        RefreshVisibleSelectionState();
    }

    private void OnTaskControlClicked(ToDoTask task)
    {
        if (_isSelectionMode)
        {
            ToggleTaskSelection(task.Id);
            return;
        }

        TaskClicked?.Invoke(task);
    }

    private void OnTaskControlSelectionChanged(TaskItemControl item, bool isSelected)
    {
        OnItemSelectionChanged(item, isSelected);
    }

    private void OnTaskContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.ItemContainer is not ListViewItem container || args.Item is not ToDoTask task)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() => ApplyTaskControlState(container, task));
    }

    private void RefreshVisibleSelectionState()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            foreach (var task in VisibleTasks)
            {
                if (TaskList.ContainerFromItem(task) is ListViewItem container)
                {
                    ApplyTaskControlState(container, task);
                }
            }
        });
    }

    private void ApplyTaskControlState(DependencyObject container, ToDoTask task)
    {
        if (FindDescendant<TaskItemControl>(container) is not TaskItemControl control)
        {
            return;
        }

        control.SetSelectionMode(_isSelectionMode);
        control.IsSelected = _selectedTaskIds.Contains(task.Id);
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            T? descendant = FindDescendant<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
