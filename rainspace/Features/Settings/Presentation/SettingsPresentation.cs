using Microsoft.UI.Xaml;
using rainspace.Control.UserControls.Config;
using rainspace.Features.Settings.Contracts;

namespace rainspace.Features.Settings.Presentation;

public interface ISettingsPageFactory
{
    bool TryCreate(string sectionId, out FrameworkElement? page);
}

public sealed class BuiltInSettingsSectionProvider : ISettingsSectionProvider
{
    public IEnumerable<SettingsSectionDescriptor> GetSections()
    {
        yield return new SettingsSectionDescriptor("Theme", "显示设置", "\uE7F4", 100);
        yield return new SettingsSectionDescriptor("Motion", "动作", "\uF8B0", 200);
        yield return new SettingsSectionDescriptor("Other", "其它", "\uECA5", 300);
    }
}

public sealed class BuiltInSettingsPageFactory : ISettingsPageFactory
{
    public bool TryCreate(string sectionId, out FrameworkElement? page)
    {
        page = sectionId switch
        {
            "Theme" => new ThemeControl(),
            "Motion" => new MotionControl(),
            "Other" => new OtherControl(),
            _ => null
        };
        return page != null;
    }
}

public sealed class SettingsPageResolver
{
    private readonly IReadOnlyList<ISettingsPageFactory> _factories;

    public SettingsPageResolver(IEnumerable<ISettingsPageFactory> factories)
    {
        _factories = factories?.ToList()
            ?? throw new ArgumentNullException(nameof(factories));
    }

    public FrameworkElement? Create(string sectionId)
    {
        foreach (ISettingsPageFactory factory in _factories)
        {
            if (factory.TryCreate(sectionId, out FrameworkElement? page))
            {
                return page;
            }
        }

        return null;
    }
}
