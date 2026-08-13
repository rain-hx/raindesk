using raindesk.Constant;
using Microsoft.UI.Xaml;

namespace raindesk.ViewModel;

/// <summary>
/// AppConfig 的窗口布局相关属性（partial class）。
/// </summary>
public partial class AppConfig
{
    // ── 窗口尺寸 ──────────────────────────────────────────────────

    private double windowWidth = (double)CommonEnum.WINDOW_WIDTH;
    private double windowHeight = (double)CommonEnum.WINDOW_HEIGHT;
    private double windowLeft = -1;
    private double windowTop = -1;
    private double menuCardWidth = (double)CommonEnum.MENU_CARD_WIDHT;
    private int pannelCornerRadius = 4;

    public double WindowWidth
    {
        get => windowWidth;
        set => SetProperty(ref windowWidth, value);
    }

    public double WindowHeight
    {
        get => windowHeight;
        set => SetProperty(ref windowHeight, value);
    }

    public double WindowLeft
    {
        get => windowLeft;
        set => SetProperty(ref windowLeft, value);
    }

    public double WindowTop
    {
        get => windowTop;
        set => SetProperty(ref windowTop, value);
    }

    public double MenuCardWidth
    {
        get => menuCardWidth;
        set => SetProperty(ref menuCardWidth, value);
    }

    public int PannelCornerRadius
    {
        get => pannelCornerRadius;
        set => SetProperty(ref pannelCornerRadius, value);
    }

    // ── 图片尺寸 ──────────────────────────────────────────────────

    private int imageWidth = (int)CommonEnum.IMAGE_WIDTH;
    private int imageHeight = (int)CommonEnum.IMAGE_HEIGHT;
    private double imgPanelWidth = (double)CommonEnum.IMAGE_PANEL_WIDTH;
    private double imgPanelHeight = (double)CommonEnum.IMAGE_PANEL_HEIGHT;

    public int ImageWidth
    {
        get => imageWidth == 0 ? (int)CommonEnum.IMAGE_WIDTH : imageWidth;
        set
        {
            imageWidth = value;
            ImageHeight = value;

            const double DefaultWidthFactor = 2.44;
            const double MinWidthFactor = 2.2;
            const double DeviationDivisor = 5d;

            double deviation = ((double)imageWidth - (double)CommonEnum.IMAGE_WIDTH) / DeviationDivisor;
            double factor = DefaultWidthFactor;
            deviation *= 2d;
            while (deviation > 1) deviation /= 10d;
            if (deviation > 0d) factor -= deviation;
            if (factor < MinWidthFactor) factor = MinWidthFactor;

            ImgPanelWidth = (int)(ImageWidth * factor);
            OnPropertyChanged();
        }
    }

    public int ImageHeight
    {
        get => imageHeight == 0 ? (int)CommonEnum.IMAGE_HEIGHT : imageHeight;
        set
        {
            imageHeight = value;

            const double DefaultHeightFactor = 2.00;
            const double MinHeightFactor = 1.5;
            const double DeviationDivisor = 5d;

            double deviation = ((double)imageHeight - (double)CommonEnum.IMAGE_HEIGHT) / DeviationDivisor;
            while (deviation > 1) deviation /= 10d;
            double factor = DefaultHeightFactor;
            if (deviation > 0d) factor -= deviation;
            if (factor < MinHeightFactor) factor = MinHeightFactor;

            ImgPanelHeight = ImageHeight * factor;
            OnPropertyChanged();
        }
    }

    public double ImgPanelWidth
    {
        get => imgPanelWidth == 0d ? (double)CommonEnum.IMAGE_PANEL_WIDTH : imgPanelWidth;
        set => SetProperty(ref imgPanelWidth, value);
    }

    public double ImgPanelHeight
    {
        get => imgPanelHeight == 0d ? (double)CommonEnum.IMAGE_PANEL_HEIGHT : imgPanelHeight;
        set => SetProperty(ref imgPanelHeight, value);
    }

    // ── 菜单布局 ──────────────────────────────────────────────────

    private int selectedMenuIndex;
    private string? selectedGroupId;
    private SortType menuSortType = SortType.CUSTOM;
    private SortType iconSortType = SortType.CUSTOM;
    private Visibility configIconVisible = Visibility.Visible;
    private Visibility titleLogoVisible = Visibility.Visible;

    public int SelectedMenuIndex
    {
        get => selectedMenuIndex;
        set => SetProperty(ref selectedMenuIndex, value);
    }

    /// <summary>
    /// G4: 当前选中分组的稳定 ID（不随重排变化）。
    /// 与 SelectedMenuIndex 双写，SelectedGroupId 为主、Index 为兼容。
    /// </summary>
    public string? SelectedGroupId
    {
        get => selectedGroupId;
        set => SetProperty(ref selectedGroupId, value);
    }

    public SortType MenuSortType
    {
        get => menuSortType;
        set => SetProperty(ref menuSortType, value);
    }

    public SortType IconSortType
    {
        get => iconSortType;
        set => SetProperty(ref iconSortType, value);
    }

    public Visibility ConfigIconVisible
    {
        get => configIconVisible;
        set => SetProperty(ref configIconVisible, value);
    }

    public Visibility TitleLogoVisible
    {
        get => titleLogoVisible;
        set => SetProperty(ref titleLogoVisible, value);
    }
}
