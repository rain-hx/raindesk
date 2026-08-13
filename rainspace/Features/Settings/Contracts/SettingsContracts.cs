namespace rainspace.Features.Settings.Contracts;

public sealed record SettingsSectionDescriptor(
    string Id,
    string Title,
    string IconGlyph,
    int Order);

/// <summary>内置模块或插件提供设置分区元数据的扩展点。</summary>
public interface ISettingsSectionProvider
{
    IEnumerable<SettingsSectionDescriptor> GetSections();
}

public interface ISettingsSectionCatalog
{
    IReadOnlyList<SettingsSectionDescriptor> Sections { get; }
}
