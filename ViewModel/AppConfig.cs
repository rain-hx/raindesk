using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// WinUI 3 兼容的热键类型
namespace raindesk.Util
{
    [Flags]
    public enum HotkeyModifiers : uint
    {
        None = 0,
        MOD_ALT = 0x0001,
        MOD_CONTROL = 0x0002,
        MOD_SHIFT = 0x0004,
        MOD_WIN = 0x0008,
        MOD_NOREPEAT = 0x4000
    }

    public enum VirtualKey : ushort
    {
        None = 0,
        A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46,
        G = 0x47, H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C,
        M = 0x4D, N = 0x4E, O = 0x4F, P = 0x50, Q = 0x51, R = 0x52,
        S = 0x53, T = 0x54, U = 0x55, V = 0x56, W = 0x57, X = 0x58,
        Y = 0x59, Z = 0x5A,
        Space = 0x20, Enter = 0x0D, Escape = 0x1B, Tab = 0x09,
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75,
        F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
    }
}

namespace raindesk.ViewModel
{

/// <summary>
/// 程序设置。
/// 使用 partial class 按功能域组织到多个文件：
///   AppConfig.Hotkey.cs  - 热键设置
///   AppConfig.Theme.cs   - 主题和外观
///   AppConfig.Layout.cs  - 窗口布局
///   AppConfig.Behavior.cs - 行为设置
/// </summary>
[Serializable]
public partial class AppConfig : INotifyPropertyChanged
{
    // ── 运行时状态（不序列化） ────────────────────────────────────

    [JsonIgnore]
    private ObservableCollection<IconInfo>? selectedMenuIcons;

    // ── 其他持久化属性 ────────────────────────────────────────────

    private string? customIconUrl;
    private string? customIconJsonUrl;
    private List<string>? allGroupsFlatIconOrderKeys;
    private ObservableCollection<IconInfo> allGroupsIconList = new();
    private string trackingUuid = "";

    // ── 属性 ──────────────────────────────────────────────────────

    [JsonIgnore]
    public ObservableCollection<IconInfo>? SelectedMenuIcons
    {
        get => selectedMenuIcons;
        set => SetProperty(ref selectedMenuIcons, value);
    }

    public string? CustomIconUrl
    {
        get => customIconUrl;
        set => SetProperty(ref customIconUrl, value);
    }

    public string? CustomIconJsonUrl
    {
        get => customIconJsonUrl;
        set => SetProperty(ref customIconJsonUrl, value);
    }

    public List<string>? AllGroupsFlatIconOrderKeys
    {
        get => allGroupsFlatIconOrderKeys;
        set => SetProperty(ref allGroupsFlatIconOrderKeys, value);
    }

    public ObservableCollection<IconInfo> AllGroupsIconList
    {
        get => allGroupsIconList;
        set => SetProperty(ref allGroupsIconList, value ?? new ObservableCollection<IconInfo>());
    }

    /// <summary>
    /// 持久化的用户追踪 UUID，用于更新统计（仅计数，不收集信息）。
    /// 首次访问时自动生成并保存。
    /// </summary>
    public string TrackingUuid
    {
        get
        {
            if (string.IsNullOrEmpty(trackingUuid))
            {
                trackingUuid = Guid.NewGuid().ToString("N");
            }
            return trackingUuid;
        }
        set
        {
            SetProperty(ref trackingUuid, value);
        }
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

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
}
