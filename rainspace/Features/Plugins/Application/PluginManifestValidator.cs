using rainspace.Features.Plugins.Contracts;

namespace rainspace.Features.Plugins.Application;

public sealed class PluginManifestValidator : IPluginManifestValidator
{
    private readonly bool _allowExternalPlugins;

    public PluginManifestValidator(bool allowExternalPlugins = false)
    {
        _allowExternalPlugins = allowExternalPlugins;
    }

    public PluginValidationResult Validate(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var errors = new List<string>();
        IReadOnlyList<PluginCapability> capabilities =
            manifest.Capabilities ?? Array.Empty<PluginCapability>();
        IReadOnlyList<PluginContributionDescriptor> contributions =
            manifest.Contributions ?? Array.Empty<PluginContributionDescriptor>();

        if (!IsValidId(manifest.Id))
        {
            errors.Add("插件 ID 只能包含小写字母、数字、点、下划线和连字符。");
        }
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("插件名称不能为空。");
        }
        if (!Version.TryParse(manifest.Version, out Version? version)
            || version.Major < 0
            || version.Minor < 0)
        {
            errors.Add("插件版本必须是有效的数字版本。");
        }
        if (manifest.ApiVersion != PluginApi.CurrentVersion)
        {
            errors.Add($"插件 API 版本 {manifest.ApiVersion} 不受支持。");
        }
        if (!manifest.IsBuiltIn && !_allowExternalPlugins)
        {
            errors.Add("当前版本仅允许 composition root 显式注册的内置插件。");
        }
        if (!manifest.IsBuiltIn && !IsSafeRelativeEntryPoint(manifest.EntryPoint))
        {
            errors.Add("外部插件入口必须是插件目录内的安全相对路径。");
        }
        if (capabilities.Any(capability => !Enum.IsDefined(capability)))
        {
            errors.Add("插件包含未知 capability。");
        }
        if (capabilities.Distinct().Count() != capabilities.Count)
        {
            errors.Add("插件 capability 不能重复。");
        }

        var contributionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (PluginContributionDescriptor contribution in contributions)
        {
            if (!IsValidId(contribution.Id) || !contributionIds.Add(contribution.Id))
            {
                errors.Add($"贡献点 ID 无效或重复: {contribution.Id}");
            }
            if (string.IsNullOrWhiteSpace(contribution.DisplayName))
            {
                errors.Add($"贡献点 {contribution.Id} 缺少显示名称。");
            }

            if (!Enum.IsDefined(contribution.Kind))
            {
                errors.Add($"贡献点 {contribution.Id} 类型未知。");
                continue;
            }

            PluginCapability requiredCapability = RequiredCapability(contribution.Kind);
            if (!capabilities.Contains(requiredCapability))
            {
                errors.Add($"贡献点 {contribution.Id} 缺少 capability {requiredCapability}。");
            }
        }

        return errors.Count == 0
            ? PluginValidationResult.Valid
            : new PluginValidationResult(false, errors);
    }

    private static PluginCapability RequiredCapability(PluginContributionKind kind) => kind switch
    {
        PluginContributionKind.SearchProvider => PluginCapability.Search,
        PluginContributionKind.SettingsSection => PluginCapability.Settings,
        PluginContributionKind.LauncherCommand => PluginCapability.LauncherCommands,
        PluginContributionKind.Importer => PluginCapability.Importers,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "未知插件贡献类型。")
    };

    private static bool IsValidId(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= 96
        && value.All(character => char.IsLower(character)
            || char.IsDigit(character)
            || character is '.' or '-' or '_');

    private static bool IsSafeRelativeEntryPoint(string? entryPoint)
    {
        if (string.IsNullOrWhiteSpace(entryPoint)
            || Path.IsPathRooted(entryPoint)
            || entryPoint.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        return !entryPoint.Contains(':')
            && !entryPoint.StartsWith(Path.DirectorySeparatorChar)
            && !entryPoint.StartsWith(Path.AltDirectorySeparatorChar);
    }
}
