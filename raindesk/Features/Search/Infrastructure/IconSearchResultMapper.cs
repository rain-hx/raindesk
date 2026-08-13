using raindesk.Constant;
using raindesk.Features.Search.Contracts;
using raindesk.ViewModel;

namespace raindesk.Features.Search.Infrastructure;

internal static class IconSearchResultMapper
{
    public static SearchResult FromIcon(IconInfo icon, string providerId)
    {
        ArgumentNullException.ThrowIfNull(icon);

        byte[]? bytes = icon.ImageByteArr_NoWrite ?? icon.DefaultImage_NoWrite;
        SearchResultKind kind = icon.IsFolder
            ? SearchResultKind.Folder
            : icon.IconType == IconType.URL
                ? SearchResultKind.Url
                : SearchResultKind.Shortcut;

        return new SearchResult(
            Id: string.IsNullOrWhiteSpace(icon.IconId) ? Guid.NewGuid().ToString("N") : icon.IconId,
            ProviderId: providerId,
            Name: icon.Name ?? string.Empty,
            Path: icon.Path ?? string.Empty,
            StartArgument: icon.StartArg,
            RunAsAdministrator: icon.AdminStartUp,
            Kind: kind,
            IconBytes: bytes == null ? ReadOnlyMemory<byte>.Empty : new ReadOnlyMemory<byte>(bytes));
    }
}
