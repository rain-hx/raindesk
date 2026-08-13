using rainspace.Features.Settings.Application;
using rainspace.Features.Settings.Contracts;

namespace rainspace.Tests;

[TestClass]
public class SettingsSectionCatalogTests
{
    [TestMethod]
    public void Catalog_OrdersValidSectionsAndKeepsFirstDuplicateId()
    {
        var first = new StubProvider(
            new SettingsSectionDescriptor("other", "其它", "o", 300),
            new SettingsSectionDescriptor("theme", "显示设置", "t", 100));
        var second = new StubProvider(
            new SettingsSectionDescriptor("THEME", "重复主题", "x", 50),
            new SettingsSectionDescriptor("plugin", "插件设置", "p", 200),
            new SettingsSectionDescriptor("", "无效", "i", 0));

        var catalog = new SettingsSectionCatalog(new ISettingsSectionProvider[] { first, second });

        CollectionAssert.AreEqual(
            new[] { "theme", "plugin", "other" },
            catalog.Sections.Select(section => section.Id).ToArray());
        Assert.AreEqual("显示设置", catalog.Sections[0].Title);
    }

    [TestMethod]
    public void Catalog_FailingProvider_DoesNotHideHealthyContributions()
    {
        var catalog = new SettingsSectionCatalog(new ISettingsSectionProvider[]
        {
            new ThrowingProvider(),
            new StubProvider(new SettingsSectionDescriptor("healthy", "可用设置", "h", 1))
        });

        Assert.AreEqual(1, catalog.Sections.Count);
        Assert.AreEqual("healthy", catalog.Sections[0].Id);
    }

    private sealed class StubProvider : ISettingsSectionProvider
    {
        private readonly IReadOnlyList<SettingsSectionDescriptor> _sections;

        public StubProvider(params SettingsSectionDescriptor[] sections)
        {
            _sections = sections;
        }

        public IEnumerable<SettingsSectionDescriptor> GetSections() => _sections;
    }

    private sealed class ThrowingProvider : ISettingsSectionProvider
    {
        public IEnumerable<SettingsSectionDescriptor> GetSections() =>
            throw new InvalidOperationException("broken plugin");
    }
}
