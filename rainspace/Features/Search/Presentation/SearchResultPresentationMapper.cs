using rainspace.Constant;
using rainspace.Features.Search.Contracts;
using rainspace.ViewModel;

namespace rainspace.Features.Search.Presentation;

/// <summary>Search 契约 DTO 到现有 WinUI 展示模型的临时映射边界。</summary>
public static class SearchResultPresentationMapper
{
    public static IconInfo ToIconInfo(SearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new IconInfo
        {
            IconId = result.Id,
            Name_NoWrite = result.Name,
            Path_NoWrite = result.Path,
            StartArg_NoWrite = result.StartArgument,
            AdminStartUp = result.RunAsAdministrator,
            IsFolder = result.Kind == SearchResultKind.Folder,
            IconType = result.Kind == SearchResultKind.Url ? IconType.URL : IconType.OTHER,
            ImageByteArr_NoWrite = result.IconBytes.IsEmpty ? null : result.IconBytes.ToArray()
        };
    }
}
