using Microsoft.UI.Xaml;
using rainspace.Features.Plugins.Contracts;
using rainspace.Features.Settings.Contracts;

namespace rainspace.Features.Settings.Presentation;

/// <summary>
/// Applies the plugin lifecycle/capability gate before exposing a settings metadata provider.
/// A failing or disabled plugin contributes no sections and cannot affect healthy providers.
/// </summary>
public sealed class PluginGatedSettingsSectionProvider : ISettingsSectionProvider
{
    private readonly IPluginContributionGate _gate;
    private readonly string _pluginId;
    private readonly string _contributionId;
    private readonly ISettingsSectionProvider _inner;

    public PluginGatedSettingsSectionProvider(
        IPluginContributionGate gate,
        string pluginId,
        string contributionId,
        ISettingsSectionProvider inner)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _pluginId = RequireId(pluginId, nameof(pluginId));
        _contributionId = RequireId(contributionId, nameof(contributionId));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public IEnumerable<SettingsSectionDescriptor> GetSections()
    {
        if (!IsEnabled())
        {
            return Array.Empty<SettingsSectionDescriptor>();
        }

        try
        {
            return (_inner.GetSections() ?? Array.Empty<SettingsSectionDescriptor>()).ToList();
        }
        catch
        {
            return Array.Empty<SettingsSectionDescriptor>();
        }
    }

    private bool IsEnabled()
    {
        try
        {
            return _gate.IsEnabled(
                _pluginId,
                _contributionId,
                PluginContributionKind.SettingsSection);
        }
        catch
        {
            return false;
        }
    }

    private static string RequireId(string value, string parameterName) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException("Plugin and contribution IDs are required.", parameterName);
}

/// <summary>
/// Applies the same gate at page creation time so a contribution disabled after catalog creation
/// cannot execute its UI factory. Factory failures are isolated to that contribution.
/// </summary>
public sealed class PluginGatedSettingsPageFactory : ISettingsPageFactory
{
    private readonly IPluginContributionGate _gate;
    private readonly string _pluginId;
    private readonly string _contributionId;
    private readonly ISettingsPageFactory _inner;

    public PluginGatedSettingsPageFactory(
        IPluginContributionGate gate,
        string pluginId,
        string contributionId,
        ISettingsPageFactory inner)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _pluginId = !string.IsNullOrWhiteSpace(pluginId)
            ? pluginId
            : throw new ArgumentException("Plugin ID is required.", nameof(pluginId));
        _contributionId = !string.IsNullOrWhiteSpace(contributionId)
            ? contributionId
            : throw new ArgumentException("Contribution ID is required.", nameof(contributionId));
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public bool TryCreate(string sectionId, out FrameworkElement? page)
    {
        page = null;
        try
        {
            if (!_gate.IsEnabled(
                    _pluginId,
                    _contributionId,
                    PluginContributionKind.SettingsSection))
            {
                return false;
            }

            return _inner.TryCreate(sectionId, out page);
        }
        catch
        {
            page = null;
            return false;
        }
    }
}
