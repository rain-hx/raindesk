using Microsoft.Windows.ApplicationModel.Resources;

namespace raindesk.Features.Appearance.Infrastructure;

/// <summary>
/// 本地化资源访问入口。
/// 资源位于 <see cref="raindesk/Strings/&lt;lang&gt;/Resources.resw"/>，
/// 未打包运行时回退到默认语言资源。
/// </summary>
public static class L10n
{
    private static readonly ResourceLoader Loader = new("Resources");

    /// <summary>
    /// 读取指定资源键的本地化字符串。键不存在时返回键名本身，便于发现遗漏。
    /// </summary>
    public static string Get(string key)
    {
        try
        {
            return Loader.GetString(key);
        }
        catch
        {
            return key;
        }
    }
}
