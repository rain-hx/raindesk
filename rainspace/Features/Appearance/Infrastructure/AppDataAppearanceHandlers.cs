using rainspace.Constant;
using rainspace.Features.Appearance.Contracts;
using rainspace.Services;
using rainspace.Services.Interfaces;
using rainspace.ViewModel;

namespace rainspace.Features.Appearance.Infrastructure;

public sealed class AppDataAppearanceQueryHandler : IAppearanceQueryHandler
{
    private readonly IAppDataProvider _dataProvider;

    public AppDataAppearanceQueryHandler(IAppDataProvider dataProvider)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public AppearanceSnapshot GetCurrent() => _dataProvider.Read(data => new AppearanceSnapshot(
        MapTheme(ThemeService.NormalizeMode(data.AppConfig.AppThemeMode)),
        data.AppConfig.BGStyle == BGStyle.GradientBac
            ? AppearanceBackgroundStyle.Gradient
            : AppearanceBackgroundStyle.Image,
        data.AppConfig.BacImgName,
        data.AppConfig.BgOpacity,
        data.AppConfig.BlurEffect,
        data.AppConfig.BlurValue,
        data.AppConfig.HideSearchBox,
        data.AppConfig.ShowIconTitle ?? true));

    public AppearanceGradientSnapshot GetGradient() => _dataProvider.Read(data =>
    {
        GradientBGParam? gradient = data.AppConfig.GradientBGParam;
        return new AppearanceGradientSnapshot(
            gradient?.Id,
            gradient?.Name ?? string.Empty,
            gradient?.Color1 ?? "#000000",
            gradient?.Color2 ?? "#000000");
    });

    public IReadOnlyList<AppearanceGradientSnapshot> GetCustomGradients() =>
        _dataProvider.Read(data => data.AppConfig.CustomBGParams
            .Select(gradient => new AppearanceGradientSnapshot(
                gradient.Id,
                gradient.Name ?? string.Empty,
                gradient.Color1 ?? "#000000",
                gradient.Color2 ?? "#000000"))
            .ToArray());

    internal static AppearanceThemeMode MapTheme(AppThemeMode mode) => mode switch
    {
        AppThemeMode.Light => AppearanceThemeMode.Light,
        AppThemeMode.Dark => AppearanceThemeMode.Dark,
        _ => AppearanceThemeMode.System
    };
}

public sealed class AppDataAppearanceCommandHandler : IAppearanceCommandHandler
{
    private readonly IAppDataProvider _dataProvider;
    private readonly ISaveService _saveService;

    public AppDataAppearanceCommandHandler(IAppDataProvider dataProvider, ISaveService saveService)
    {
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
    }

    public AppearanceCommandResult SetTheme(SetAppearanceThemeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Mode))
        {
            return AppearanceCommandResult.Failed("主题模式无效。");
        }

        AppThemeMode mode = command.Mode switch
        {
            AppearanceThemeMode.Light => AppThemeMode.Light,
            AppearanceThemeMode.Dark => AppThemeMode.Dark,
            _ => AppThemeMode.System
        };
        bool changed = false;
        _dataProvider.Write(data =>
        {
            string value = ThemeService.ToPersistedValue(mode);
            changed = !string.Equals(data.AppConfig.AppThemeMode, value, StringComparison.Ordinal);
            data.AppConfig.AppThemeMode = value;
        });
        ThemeService.SetMode(mode);
        SaveIfChanged(changed);
        return new AppearanceCommandResult(true, changed);
    }

    public AppearanceCommandResult SetBackgroundImage(SetBackgroundImageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.ImageBytes == null || command.ImageBytes.Length == 0)
        {
            return AppearanceCommandResult.Failed("背景图片不能为空。");
        }

        byte[] image = command.ImageBytes.ToArray();
        string displayName = string.IsNullOrWhiteSpace(command.DisplayName)
            ? "自定义背景"
            : command.DisplayName.Trim();
        _dataProvider.Write(data =>
        {
            data.AppConfig.ImageByteArr = image;
            data.AppConfig.BacImgName = displayName;
        });
        _saveService.SaveDebounced();
        return new AppearanceCommandResult(true, true);
    }

    public AppearanceCommandResult SetBackgroundStyle(SetBackgroundStyleCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Style))
        {
            return AppearanceCommandResult.Failed("背景样式无效。");
        }

        BGStyle style = command.Style == AppearanceBackgroundStyle.Gradient
            ? BGStyle.GradientBac
            : BGStyle.ImgBac;
        bool changed = false;
        _dataProvider.Write(data =>
        {
            changed = data.AppConfig.BGStyle != style;
            data.AppConfig.BGStyle = style;
        });
        SaveIfChanged(changed);
        return new AppearanceCommandResult(true, changed);
    }

    public AppearanceCommandResult UpdateEffects(UpdateAppearanceEffectsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.BackgroundOpacity is < 0 or > 100
            || command.BlurAmount is < 0 or > 100)
        {
            return AppearanceCommandResult.Failed("透明度和模糊值必须在 0 到 100 之间。");
        }

        bool changed = false;
        _dataProvider.Write(data =>
        {
            var config = data.AppConfig;
            changed = config.BgOpacity != command.BackgroundOpacity
                || config.BlurEffect != command.BlurEnabled
                || Math.Abs(config.BlurValue - command.BlurAmount) > 0.001
                || config.HideSearchBox != command.HideSearchBox;
            config.BgOpacity = command.BackgroundOpacity;
            config.BlurEffect = command.BlurEnabled;
            config.BlurValue = command.BlurEnabled ? command.BlurAmount : 0;
            config.HideSearchBox = command.HideSearchBox;
        });
        SaveIfChanged(changed);
        return new AppearanceCommandResult(true, changed);
    }

    public AppearanceCommandResult SetIconTitleVisibility(SetIconTitleVisibilityCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        bool changed = false;
        _dataProvider.Write(data =>
        {
            bool current = data.AppConfig.ShowIconTitle ?? true;
            changed = current != command.IsVisible
                || data.AppConfig.ShowIconTitle == null;
            if (changed)
            {
                data.AppConfig.ShowIconTitle = command.IsVisible;
            }
        });
        SaveIfChanged(changed);
        return new AppearanceCommandResult(true, changed);
    }

    public AppearanceCommandResult SetColor(SetAppearanceColorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!Enum.IsDefined(command.Target) || !IsColor(command.Color))
        {
            return AppearanceCommandResult.Failed("颜色值无效。");
        }

        bool changed = false;
        _dataProvider.Write(data =>
        {
            AppConfig config = data.AppConfig;
            config.GradientBGParam ??= new GradientBGParam(string.Empty, string.Empty, "#000000", "#000000");
            switch (command.Target)
            {
                case AppearanceColorTarget.GradientStart:
                    changed = !string.Equals(config.GradientBGParam.Color1, command.Color, StringComparison.OrdinalIgnoreCase);
                    config.GradientBGParam.Color1 = command.Color;
                    break;
                case AppearanceColorTarget.GradientEnd:
                    changed = !string.Equals(config.GradientBGParam.Color2, command.Color, StringComparison.OrdinalIgnoreCase);
                    config.GradientBGParam.Color2 = command.Color;
                    break;
                default:
                    changed = !string.Equals(config.TextColor, command.Color, StringComparison.OrdinalIgnoreCase);
                    config.TextColor = command.Color;
                    break;
            }
        });
        SaveIfChanged(changed);
        return new AppearanceCommandResult(true, changed);
    }

    public AppearanceCommandResult SelectGradient(SelectAppearanceGradientCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!IsColor(command.StartColor) || !IsColor(command.EndColor))
        {
            return AppearanceCommandResult.Failed("渐变颜色值无效。");
        }

        _dataProvider.Write(data => data.AppConfig.GradientBGParam = new GradientBGParam(
            command.Id ?? string.Empty,
            command.Name?.Trim() ?? string.Empty,
            command.StartColor,
            command.EndColor));
        _saveService.SaveDebounced();
        return new AppearanceCommandResult(true, true);
    }

    public AppearanceCommandResult AddCustomGradient(AddCustomAppearanceGradientCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.Name)
            || !IsColor(command.StartColor)
            || !IsColor(command.EndColor))
        {
            return AppearanceCommandResult.Failed("名称和渐变颜色不能为空。");
        }

        _dataProvider.Write(data => data.AppConfig.CustomBGParams.Add(new GradientBGParam(
            Guid.NewGuid().ToString("N"),
            command.Name.Trim(),
            command.StartColor,
            command.EndColor)));
        _saveService.SaveDebounced();
        return new AppearanceCommandResult(true, true);
    }

    public AppearanceCommandResult DeleteCustomGradient(DeleteCustomAppearanceGradientCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        bool removed = false;
        _dataProvider.Write(data =>
        {
            for (int i = data.AppConfig.CustomBGParams.Count - 1; i >= 0; i--)
            {
                GradientBGParam candidate = data.AppConfig.CustomBGParams[i];
                bool matches = !string.IsNullOrWhiteSpace(command.Id)
                    ? string.Equals(candidate.Id, command.Id, StringComparison.Ordinal)
                    : string.IsNullOrWhiteSpace(candidate.Id)
                        && string.Equals(candidate.Color1, command.StartColor, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(candidate.Color2, command.EndColor, StringComparison.OrdinalIgnoreCase);
                if (matches)
                {
                    data.AppConfig.CustomBGParams.RemoveAt(i);
                    removed = true;
                }
            }
        });
        SaveIfChanged(removed);
        return new AppearanceCommandResult(true, removed);
    }

    private static bool IsColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)
            || color[0] != '#'
            || (color.Length != 7 && color.Length != 9))
        {
            return false;
        }

        return color.AsSpan(1).ToString().All(Uri.IsHexDigit);
    }

    private void SaveIfChanged(bool changed)
    {
        if (changed)
        {
            _saveService.SaveDebounced();
        }
    }
}
