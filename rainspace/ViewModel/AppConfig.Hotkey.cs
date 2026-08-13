using rainspace.Util;

namespace rainspace.ViewModel;

/// <summary>
/// AppConfig 的热键相关属性（partial class）。
/// </summary>
public partial class AppConfig
{
    // ── 主面板热键 ────────────────────────────────────────────────

    private string hotkeyStr = "Ctrl + Q";
    private HotkeyModifiers hotkeyModifiers = HotkeyModifiers.MOD_CONTROL;
    private VirtualKey hotkey = VirtualKey.Q;
    private bool? enableAppHotKey = true;

    public string HotkeyStr
    {
        get => hotkeyStr;
        set => SetProperty(ref hotkeyStr, value);
    }

    public HotkeyModifiers HotkeyModifiers
    {
        get
        {
            if (hotkeyModifiers == 0) hotkeyModifiers = HotkeyModifiers.MOD_CONTROL;
            return hotkeyModifiers;
        }
        set => SetProperty(ref hotkeyModifiers, value);
    }

    public HotkeyModifiers HotkeyModifiers_NoWrite
    {
        get => hotkeyModifiers;
        set => hotkeyModifiers = value;
    }

    public VirtualKey Hotkey
    {
        get => hotkey;
        set => SetProperty(ref hotkey, value);
    }

    public VirtualKey Hotkey_NoWrite
    {
        get => hotkey;
        set => hotkey = value;
    }

    public bool? EnableAppHotKey
    {
        get => enableAppHotKey;
        set => SetProperty(ref enableAppHotKey, value);
    }

    // ── 待办热键 ──────────────────────────────────────────────────

    private string toDoHotkeyStr = "Ctrl + Shift + Q";
    private HotkeyModifiers toDoHotkeyModifiers;
    private VirtualKey toDoHotkey = VirtualKey.Q;
    private bool? enableTodoHotKey = true;

    public string ToDoHotkeyStr
    {
        get => toDoHotkeyStr;
        set => SetProperty(ref toDoHotkeyStr, value);
    }

    public HotkeyModifiers ToDoHotkeyModifiers
    {
        get
        {
            if (toDoHotkeyModifiers == 0)
                toDoHotkeyModifiers = HotkeyModifiers.MOD_CONTROL | HotkeyModifiers.MOD_SHIFT;
            return toDoHotkeyModifiers;
        }
        set => SetProperty(ref toDoHotkeyModifiers, value);
    }

    public HotkeyModifiers ToDoHotkeyModifiers_NoWrite
    {
        get => toDoHotkeyModifiers;
        set => toDoHotkeyModifiers = value;
    }

    public VirtualKey ToDoHotkey
    {
        get
        {
            if (toDoHotkey == VirtualKey.None) toDoHotkey = VirtualKey.E;
            return toDoHotkey;
        }
        set => SetProperty(ref toDoHotkey, value);
    }

    public VirtualKey ToDoHotkey_NoWrite
    {
        get => toDoHotkey;
        set => toDoHotkey = value;
    }

    public bool? EnableTodoHotKey
    {
        get => enableTodoHotKey;
        set => SetProperty(ref enableTodoHotKey, value);
    }
}
