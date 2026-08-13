using System;
using System.Collections.Generic;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// P2: 每帧统一的拖拽解析结果。一次计算同时得到文件夹目标、换位索引和分组目标。
/// </summary>
internal sealed class LauncherDragFrameResult
{
    /// <summary>文件夹放置目标（null 表示无文件夹意图）。</summary>
    public IconInfo? FolderDropTarget { get; set; }

    /// <summary>换位目标索引（-1 表示无换位）。</summary>
    public int ReorderTargetIndex { get; set; } = -1;

    /// <summary>是否需要更新拖拽预览位置。</summary>
    public bool NeedsPreviewUpdate { get; set; }

    /// <summary>是否命中了文件夹目标。</summary>
    public bool HasFolderTarget => FolderDropTarget != null;

    /// <summary>是否命中了换位目标。</summary>
    public bool HasReorderTarget => ReorderTargetIndex >= 0;

    /// <summary>帧时间戳（用于去重）。</summary>
    public long FrameTick { get; set; }
}

