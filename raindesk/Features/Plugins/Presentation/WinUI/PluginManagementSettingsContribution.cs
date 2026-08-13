using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using raindesk.Features.Plugins.Contracts;
using raindesk.Features.Settings.Contracts;
using raindesk.Features.Settings.Presentation;

namespace raindesk.Features.Plugins.Presentation.WinUI;

public static class PluginManagementSettingsContribution
{
    public const string PluginId = "raindesk.plugin-management";
    public const string ContributionId = "plugin-management-settings";
    public const string SectionId = "Plugins";
}

public sealed class PluginManagementSettingsSectionProvider : ISettingsSectionProvider
{
    public IEnumerable<SettingsSectionDescriptor> GetSections()
    {
        yield return new SettingsSectionDescriptor(
            PluginManagementSettingsContribution.SectionId,
            "插件",
            "\uE74C",
            250);
    }
}

public sealed class PluginManagementSettingsPageFactory : ISettingsPageFactory
{
    private readonly IPluginCatalog _catalog;

    public PluginManagementSettingsPageFactory(IPluginCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public bool TryCreate(string sectionId, out FrameworkElement? page)
    {
        if (!string.Equals(
                sectionId,
                PluginManagementSettingsContribution.SectionId,
                StringComparison.OrdinalIgnoreCase))
        {
            page = null;
            return false;
        }

        page = new PluginManagementSettingsPage(_catalog.GetPlugins());
        return true;
    }
}

internal sealed class PluginManagementSettingsPage : UserControl
{
    public PluginManagementSettingsPage(IReadOnlyList<PluginDescriptor> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var content = new StackPanel
        {
            Spacing = 14,
            Padding = new Thickness(8, 4, 8, 16)
        };
        content.Children.Add(new TextBlock
        {
            Text = "插件与扩展",
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        content.Children.Add(new TextBlock
        {
            Text = "当前仅启用由应用显式注册的内置插件。贡献点必须通过能力与生命周期门禁后才能运行。",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.72
        });

        if (plugins.Count == 0)
        {
            content.Children.Add(new TextBlock
            {
                Text = "暂无已注册插件。",
                Opacity = 0.72
            });
        }
        else
        {
            foreach (PluginDescriptor plugin in plugins)
            {
                content.Children.Add(CreatePluginCard(plugin));
            }
        }

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
    }

    private static FrameworkElement CreatePluginCard(PluginDescriptor plugin)
    {
        string capabilities = plugin.Manifest.Capabilities.Count == 0
            ? "无"
            : string.Join(" · ", plugin.Manifest.Capabilities);
        string state = plugin.State switch
        {
            PluginState.Enabled => "已启用",
            PluginState.Disabled => "已禁用",
            PluginState.Failed => "启动失败",
            _ => "已发现"
        };

        var details = new StackPanel { Spacing = 5 };
        details.Children.Add(new TextBlock
        {
            Text = $"{plugin.Manifest.Name}  {plugin.Manifest.Version}",
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        });
        details.Children.Add(new TextBlock
        {
            Text = $"状态：{state}    能力：{capabilities}",
            TextWrapping = TextWrapping.Wrap,
            Opacity = 0.76
        });
        if (!string.IsNullOrWhiteSpace(plugin.Error))
        {
            details.Children.Add(new TextBlock
            {
                Text = plugin.Error,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.76
            });
        }

        return new Border
        {
            Padding = new Thickness(14, 12, 14, 12),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            Child = details
        };
    }
}
