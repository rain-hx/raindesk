using rainspace.Features.Settings.Contracts;

namespace rainspace.Features.Settings.Application;

/// <summary>聚合并校验设置贡献点；相同 ID 只接纳第一个注册项。</summary>
public sealed class SettingsSectionCatalog : ISettingsSectionCatalog
{
    public SettingsSectionCatalog(IEnumerable<ISettingsSectionProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Sections = EnumerateSafely(providers)
            .Where(section => !string.IsNullOrWhiteSpace(section.Id)
                              && !string.IsNullOrWhiteSpace(section.Title))
            .Where(section => seen.Add(section.Id))
            .OrderBy(section => section.Order)
            .ThenBy(section => section.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<SettingsSectionDescriptor> Sections { get; }

    private static IEnumerable<SettingsSectionDescriptor> EnumerateSafely(
        IEnumerable<ISettingsSectionProvider> providers)
    {
        foreach (ISettingsSectionProvider provider in providers)
        {
            IReadOnlyList<SettingsSectionDescriptor> sections;
            try
            {
                sections = (provider.GetSections() ?? Array.Empty<SettingsSectionDescriptor>()).ToList();
            }
            catch
            {
                continue;
            }

            foreach (SettingsSectionDescriptor section in sections)
            {
                yield return section;
            }
        }
    }
}
