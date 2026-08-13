using raindesk.Constant;
using raindesk.Services;
using raindesk.Util;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace raindesk.ViewModel;

/// <summary>
/// AppConfig 的主题和外观相关属性（partial class）。
/// </summary>
public partial class AppConfig
{
    // ── 主题 ──────────────────────────────────────────────────────

    private string appThemeMode = "System";

    public string AppThemeMode
    {
        get => ThemeService.ToPersistedValue(ThemeService.NormalizeMode(appThemeMode));
        set
        {
            string normalizedValue = ThemeService.ToPersistedValue(ThemeService.NormalizeMode(value));
            if (appThemeMode == normalizedValue) return;
            appThemeMode = normalizedValue;
            OnPropertyChanged();
        }
    }

    // ── 背景 ──────────────────────────────────────────────────────

    private BGStyle bgStyle = BGStyle.ImgBac;
    private byte[]? imageByteArr;
    private string bacImgName = "系统默认";
    private GradientBGParam? gradientBGParam;
    private ObservableCollection<GradientBGParam>? customBGParams;

    [JsonIgnore]
    private BitmapImage? bitmapImage;

    public BGStyle BGStyle
    {
        get
        {
            if (bgStyle == 0) bgStyle = (BGStyle)1;
            return bgStyle;
        }
        set => SetProperty(ref bgStyle, value);
    }

    public byte[]? ImageByteArr
    {
        get => imageByteArr;
        set
        {
            if (ReferenceEquals(imageByteArr, value))
            {
                return;
            }

            imageByteArr = value;
            bitmapImage = null; // 使缓存失效
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public BitmapImage? BitmapImage
    {
        get
        {
            if (bitmapImage != null) return bitmapImage;

            if (imageByteArr == null || imageByteArr.Length == 0)
            {
                bacImgName = "系统默认";
                bitmapImage = ImageUtil.ByteArrToImage(Convert.FromBase64String(Constants.DEFAULT_BAC_IMAGE_BASE64));
            }
            else
            {
                bitmapImage = ImageUtil.ByteArrToImage(imageByteArr);
            }
            return bitmapImage;
        }
        set => SetProperty(ref bitmapImage, value);
    }

    public string BacImgName
    {
        get => bacImgName;
        set => SetProperty(ref bacImgName, value);
    }

    public GradientBGParam? GradientBGParam
    {
        get => gradientBGParam;
        set => SetProperty(ref gradientBGParam, value);
    }

    public ObservableCollection<GradientBGParam> CustomBGParams
    {
        get => customBGParams ??= new();
        set => SetProperty(ref customBGParams, value);
    }

    // ── 透明度 ────────────────────────────────────────────────────

    private int cardOpacity = 10;
    private int bgOpacity = 100;
    private int pannelOpacity = 100;

    public int CardOpacity
    {
        get => cardOpacity;
        set => SetProperty(ref cardOpacity, value);
    }

    public int BgOpacity
    {
        get => bgOpacity;
        set => SetProperty(ref bgOpacity, value);
    }

    public int PannelOpacity
    {
        get => pannelOpacity;
        set => SetProperty(ref pannelOpacity, value);
    }

    // ── 文字颜色 ──────────────────────────────────────────────────

    private string textColor = "#000000";

    public string TextColor
    {
        get => textColor ?? "#000000";
        set
        {
            if (EqualityComparer<string?>.Default.Equals(textColor, value))
            {
                return;
            }

            textColor = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TextColorBrush));
        }
    }

    [JsonIgnore]
    public Microsoft.UI.Xaml.Media.SolidColorBrush TextColorBrush
    {
        get
        {
            try
            {
                string hex = TextColor.TrimStart('#');
                if (hex.Length == 6)
                {
                    byte r = Convert.ToByte(hex[..2], 16);
                    byte g = Convert.ToByte(hex[2..4], 16);
                    byte b = Convert.ToByte(hex[4..6], 16);
                    return new(global::Windows.UI.Color.FromArgb(255, r, g, b));
                }
                return new(Microsoft.UI.Colors.Black);
            }
            catch
            {
                return new(Microsoft.UI.Colors.Black);
            }
        }
    }

    // ── 毛玻璃 ────────────────────────────────────────────────────

    private bool blurEffect = true;
    private double blurValue;

    public bool BlurEffect
    {
        get => blurEffect;
        set
        {
            blurEffect = value;
            if (!value) BlurValue = 0;
            OnPropertyChanged();
        }
    }

    public double BlurValue
    {
        get => blurValue;
        set => SetProperty(ref blurValue, value);
    }
}
