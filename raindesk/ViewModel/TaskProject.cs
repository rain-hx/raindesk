using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace raindesk.ViewModel;

/// <summary>
/// 任务项目模型（参考 TaskTrove 的 ProjectSchema）。
/// 用于将任务分组到不同的项目中。
/// </summary>
[Serializable]
public class TaskProject : INotifyPropertyChanged
{
    private string id = Guid.NewGuid().ToString("N");
    private string name = string.Empty;
    private string color = "#3498DB";
    private int sortOrder;

    /// <summary>项目 ID</summary>
    public string Id
    {
        get => id;
        set => SetProperty(ref id, value);
    }

    /// <summary>项目名称</summary>
    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    /// <summary>项目颜色（十六进制）</summary>
    public string Color
    {
        get => color;
        set
        {
            if (SetProperty(ref color, value))
            {
                OnPropertyChanged(nameof(ColorBrush));
            }
        }
    }

    /// <summary>排序权重</summary>
    public int SortOrder
    {
        get => sortOrder;
        set => SetProperty(ref sortOrder, value);
    }

    /// <summary>颜色画笔（用于 UI 绑定）</summary>
    [JsonIgnore]
    public string ColorBrush => Color;

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

    // ── 预设项目 ──────────────────────────────────────────────────

    /// <summary>收件箱（默认项目）</summary>
    public static TaskProject Inbox => new()
    {
        Id = "inbox",
        Name = "收件箱",
        Color = "#3498DB"
    };

    /// <summary>工作项目</summary>
    public static TaskProject Work => new()
    {
        Id = "work",
        Name = "工作",
        Color = "#E67E22"
    };

    /// <summary>个人项目</summary>
    public static TaskProject Personal => new()
    {
        Id = "personal",
        Name = "个人",
        Color = "#2ECC71"
    };
}
