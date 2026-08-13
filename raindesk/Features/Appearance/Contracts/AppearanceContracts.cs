namespace raindesk.Features.Appearance.Contracts;

public enum AppearanceThemeMode
{
    System,
    Light,
    Dark
}

public enum AppearanceBackgroundStyle
{
    Image,
    Gradient
}

public enum AppearanceColorTarget
{
    GradientStart,
    GradientEnd,
    Text
}

public sealed record AppearanceSnapshot(
    AppearanceThemeMode ThemeMode,
    AppearanceBackgroundStyle BackgroundStyle,
    string BackgroundName,
    int BackgroundOpacity,
    bool BlurEnabled,
    double BlurAmount,
    bool HideSearchBox,
    bool ShowIconTitles);

public sealed record AppearanceGradientSnapshot(
    string? Id,
    string Name,
    string StartColor,
    string EndColor);

public sealed record SetAppearanceThemeCommand(AppearanceThemeMode Mode);

public sealed record SetBackgroundImageCommand(byte[] ImageBytes, string DisplayName);

public sealed record SetBackgroundStyleCommand(AppearanceBackgroundStyle Style);

public sealed record UpdateAppearanceEffectsCommand(
    int BackgroundOpacity,
    bool BlurEnabled,
    double BlurAmount,
    bool HideSearchBox);

public sealed record SetIconTitleVisibilityCommand(bool IsVisible);

public sealed record SetAppearanceColorCommand(
    AppearanceColorTarget Target,
    string Color);

public sealed record SelectAppearanceGradientCommand(
    string? Id,
    string Name,
    string StartColor,
    string EndColor);

public sealed record AddCustomAppearanceGradientCommand(
    string Name,
    string StartColor,
    string EndColor);

public sealed record DeleteCustomAppearanceGradientCommand(
    string? Id,
    string StartColor,
    string EndColor);

public sealed record AppearanceCommandResult(bool Success, bool Changed, string? Error = null)
{
    public static AppearanceCommandResult Failed(string error) => new(false, false, error);
}

public interface IAppearanceQueryHandler
{
    AppearanceSnapshot GetCurrent();

    AppearanceGradientSnapshot GetGradient();

    IReadOnlyList<AppearanceGradientSnapshot> GetCustomGradients();
}

public interface IAppearanceCommandHandler
{
    AppearanceCommandResult SetTheme(SetAppearanceThemeCommand command);

    AppearanceCommandResult SetBackgroundImage(SetBackgroundImageCommand command);

    AppearanceCommandResult SetBackgroundStyle(SetBackgroundStyleCommand command);

    AppearanceCommandResult UpdateEffects(UpdateAppearanceEffectsCommand command);

    AppearanceCommandResult SetIconTitleVisibility(SetIconTitleVisibilityCommand command);

    AppearanceCommandResult SetColor(SetAppearanceColorCommand command);

    AppearanceCommandResult SelectGradient(SelectAppearanceGradientCommand command);

    AppearanceCommandResult AddCustomGradient(AddCustomAppearanceGradientCommand command);

    AppearanceCommandResult DeleteCustomGradient(DeleteCustomAppearanceGradientCommand command);
}
