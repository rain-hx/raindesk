using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace raindesk.ViewModel;

/// <summary>
/// 子任务模型（参考 TaskTrove 的 Subtask）。
/// 嵌入在 ToDoTask 中，不独立持久化。
/// </summary>
[Serializable]
public class Subtask : INotifyPropertyChanged
{
    private string id = Guid.NewGuid().ToString("N");
    private string title = string.Empty;
    private bool isCompleted;
    private int sortOrder;

    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    public string Title
    {
        get => title;
        set => SetProperty(ref title, value);
    }

    public bool IsCompleted
    {
        get => isCompleted;
        set => SetProperty(ref isCompleted, value);
    }

    public int SortOrder
    {
        get => sortOrder;
        set => SetProperty(ref sortOrder, value);
    }

    // ── INotifyPropertyChanged ────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Subtask Clone()
    {
        return new Subtask
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = Title,
            IsCompleted = false,
            SortOrder = SortOrder
        };
    }
}
