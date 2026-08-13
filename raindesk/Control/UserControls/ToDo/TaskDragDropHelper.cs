using Microsoft.UI.Xaml.Controls;
using raindesk.Services.Interfaces;
using raindesk.ViewModel;
using System;
using System.Linq;

namespace raindesk.Control.UserControls.ToDo;

/// <summary>
/// 任务拖拽排序辅助类。
/// 利用 ListView 内置拖拽重排能力，在拖拽完成后同步 SortOrder 到数据层。
/// </summary>
public sealed class TaskDragDropHelper : IDisposable
{
    private readonly ListView _listView;
    private readonly IToDoService _todoService;
    private ToDoTask? _draggedTask;

    public TaskDragDropHelper(ListView listView, IToDoService todoService)
    {
        _listView = listView ?? throw new ArgumentNullException(nameof(listView));
        _todoService = todoService ?? throw new ArgumentNullException(nameof(todoService));

        SetupDragDrop();
    }

    /// <summary>拖拽排序完成事件</summary>
    public event Action? TaskReordered;

    public void Dispose()
    {
        _listView.DragItemsStarting -= OnDragItemsStarting;
        _listView.DragItemsCompleted -= OnDragItemsCompleted;
    }

    private void SetupDragDrop()
    {
        _listView.CanDragItems = true;
        _listView.CanReorderItems = true;
        _listView.AllowDrop = true;

        _listView.DragItemsStarting += OnDragItemsStarting;
        _listView.DragItemsCompleted += OnDragItemsCompleted;
    }

    private void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is ToDoTask task)
        {
            _draggedTask = task;
            e.Data.SetText(task.Id);
            e.Data.RequestedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }
    }

    private void OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        if (_draggedTask == null)
        {
            _draggedTask = null;
            return;
        }

        SyncSortOrder();
        _draggedTask = null;
        TaskReordered?.Invoke();
    }

    private void SyncSortOrder()
    {
        var items = _listView.Items
            .OfType<ToDoTask>()
            .ToList();

        for (int i = 0; i < items.Count; i++)
        {
            var task = items[i];
            if (task.SortOrder != i)
            {
                _todoService.Reorder(task.Id, i);
            }
        }
    }
}
