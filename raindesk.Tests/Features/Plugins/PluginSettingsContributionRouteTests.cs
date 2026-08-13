using Microsoft.UI.Xaml;
using raindesk.Features.Plugins.Contracts;
using raindesk.Features.Settings.Contracts;
using raindesk.Features.Settings.Presentation;

namespace raindesk.Tests.Features.Plugins;

[TestClass]
public class PluginSettingsContributionRouteTests
{
    [TestMethod]
    public void SectionProvider_EnabledSettingsContribution_ExposesInnerMetadata()
    {
        var inner = new CountingSectionProvider();
        var provider = new PluginGatedSettingsSectionProvider(
            new StubGate(enabled: true),
            "plugin",
            "settings",
            inner);

        SettingsSectionDescriptor[] sections = provider.GetSections().ToArray();

        Assert.AreEqual(1, sections.Length);
        Assert.AreEqual("plugin-section", sections[0].Id);
        Assert.AreEqual(1, inner.CallCount);
    }

    [TestMethod]
    public void SectionProvider_DisabledContribution_DoesNotInvokePluginCode()
    {
        var inner = new CountingSectionProvider();
        var provider = new PluginGatedSettingsSectionProvider(
            new StubGate(enabled: false),
            "plugin",
            "settings",
            inner);

        SettingsSectionDescriptor[] sections = provider.GetSections().ToArray();

        Assert.AreEqual(0, sections.Length);
        Assert.AreEqual(0, inner.CallCount);
    }

    [TestMethod]
    public void SectionProvider_FailingPlugin_IsIsolated()
    {
        var provider = new PluginGatedSettingsSectionProvider(
            new StubGate(enabled: true),
            "plugin",
            "settings",
            new ThrowingSectionProvider());

        SettingsSectionDescriptor[] sections = provider.GetSections().ToArray();

        Assert.AreEqual(0, sections.Length);
    }

    [TestMethod]
    public void PageFactory_DisabledContribution_DoesNotInvokePluginCode()
    {
        var inner = new ThrowingPageFactory();
        var factory = new PluginGatedSettingsPageFactory(
            new StubGate(enabled: false),
            "plugin",
            "settings",
            inner);

        bool created = factory.TryCreate("plugin-section", out FrameworkElement? page);

        Assert.IsFalse(created);
        Assert.IsNull(page);
        Assert.AreEqual(0, inner.CallCount);
    }

    [TestMethod]
    public void PageFactory_FailingEnabledPlugin_IsIsolated()
    {
        var inner = new ThrowingPageFactory();
        var factory = new PluginGatedSettingsPageFactory(
            new StubGate(enabled: true),
            "plugin",
            "settings",
            inner);

        bool created = factory.TryCreate("plugin-section", out FrameworkElement? page);

        Assert.IsFalse(created);
        Assert.IsNull(page);
        Assert.AreEqual(1, inner.CallCount);
    }

    private sealed class StubGate : IPluginContributionGate
    {
        private readonly bool _enabled;

        public StubGate(bool enabled)
        {
            _enabled = enabled;
        }

        public bool IsEnabled(
            string pluginId,
            string contributionId,
            PluginContributionKind contributionKind) =>
            _enabled
            && pluginId == "plugin"
            && contributionId == "settings"
            && contributionKind == PluginContributionKind.SettingsSection;
    }

    private sealed class CountingSectionProvider : ISettingsSectionProvider
    {
        public int CallCount { get; private set; }

        public IEnumerable<SettingsSectionDescriptor> GetSections()
        {
            CallCount++;
            yield return new SettingsSectionDescriptor(
                "plugin-section",
                "Plugin",
                "p",
                1);
        }
    }

    private sealed class ThrowingSectionProvider : ISettingsSectionProvider
    {
        public IEnumerable<SettingsSectionDescriptor> GetSections() =>
            throw new InvalidOperationException("broken settings contribution");
    }

    private sealed class ThrowingPageFactory : ISettingsPageFactory
    {
        public int CallCount { get; private set; }

        public bool TryCreate(string sectionId, out FrameworkElement? page)
        {
            CallCount++;
            throw new InvalidOperationException("broken settings page");
        }
    }
}
