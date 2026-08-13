using rainspace.Constant;

namespace rainspace.ViewModel;

/// <summary>
/// AppConfig 的行为设置相关属性（partial class）。
/// </summary>
public partial class AppConfig
{
    // ── 面板行为 ──────────────────────────────────────────────────

    private bool followMouse = true;
    private AppHideType appHideType = AppHideType.START_EXE;
    private bool startedShowPanel = true;
    private bool showBarIcon = true;
    private bool doubleOpen;
    private bool hoverMenu;
    private bool appAnimation = true;
    private bool hideSearchBox;

    public bool FollowMouse
    {
        get => followMouse;
        set => SetProperty(ref followMouse, value);
    }

    public AppHideType AppHideType
    {
        get => appHideType;
        set => SetProperty(ref appHideType, value);
    }

    public bool StartedShowPanel
    {
        get => startedShowPanel;
        set => SetProperty(ref startedShowPanel, value);
    }

    public bool ShowBarIcon
    {
        get => showBarIcon;
        set => SetProperty(ref showBarIcon, value);
    }

    public bool DoubleOpen
    {
        get => doubleOpen;
        set => SetProperty(ref doubleOpen, value);
    }

    public bool HoverMenu
    {
        get => hoverMenu;
        set => SetProperty(ref hoverMenu, value);
    }

    public bool AppAnimation
    {
        get => appAnimation;
        set => SetProperty(ref appAnimation, value);
    }

    public bool HideSearchBox
    {
        get => hideSearchBox;
        set => SetProperty(ref hideSearchBox, value);
    }

    // ── 系统集成 ──────────────────────────────────────────────────

    private bool selfStartUp = true;
    private bool selfStartUped;
    private bool pmModel;
    private UpdateType updateType = UpdateType.Gitee;

    public bool SelfStartUp
    {
        get => selfStartUp;
        set
        {
            selfStartUp = value;
            selfStartUped = true;
            OnPropertyChanged();
        }
    }

    public bool SelfStartUped
    {
        get => selfStartUped;
        set => SetProperty(ref selfStartUped, value);
    }

    public bool PMModel
    {
        get => pmModel;
        set => SetProperty(ref pmModel, value);
    }

    public UpdateType UpdateType
    {
        get => updateType;
        set => SetProperty(ref updateType, value);
    }

    // ── 其他 ──────────────────────────────────────────────────────

    private SearchType searchType;
    private string? sysBakTime;
    private string? menuPassword;
    private string? passwordHint;
    private bool? isShow;
    private bool itemSpradeAnimation = true;
    private bool? secondsWindow;
    private bool? enableEveryThing;
    private bool? alwaysTopmost;
    private bool? showIconTitle = true;

    public SearchType SearchType
    {
        get => searchType;
        set => SetProperty(ref searchType, value);
    }

    public string? SysBakTime
    {
        get => sysBakTime;
        set => SetProperty(ref sysBakTime, value);
    }

    public string? MenuPassword
    {
        get => menuPassword;
        set => SetProperty(ref menuPassword, value);
    }

    public string? PasswordHint
    {
        get => passwordHint;
        set => SetProperty(ref passwordHint, value);
    }

    public bool? IsShow
    {
        get => isShow;
        set => SetProperty(ref isShow, value);
    }

    public bool ItemSpradeAnimation
    {
        get => itemSpradeAnimation;
        set => SetProperty(ref itemSpradeAnimation, value);
    }

    public bool? SecondsWindow
    {
        get => secondsWindow;
        set => SetProperty(ref secondsWindow, value);
    }

    public bool? EnableEveryThing
    {
        get => enableEveryThing;
        set => SetProperty(ref enableEveryThing, value);
    }

    public bool? AlwaysTopmost
    {
        get => alwaysTopmost;
        set => SetProperty(ref alwaysTopmost, value);
    }

    public bool? ShowIconTitle
    {
        get => showIconTitle;
        set => SetProperty(ref showIconTitle, value);
    }
}
