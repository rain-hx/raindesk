using System;
using System.Collections.Generic;
using raindesk.Features.Workspace.Presentation;
using raindesk.ViewModel;

namespace raindesk.Features.Launcher.Presentation;

/// <summary>
/// Aggregates all pure drag-session state for Launcher icon drags.
/// Replaces the scattered fields previously on the page.
/// Does not depend on WinUI controls, timers, or persistence.
/// </summary>
internal sealed class LauncherDragSessionState
{
    // ── Primary drag state ──────────────────────────────────────────
    public bool IsCustomDragActive { get; set; }
    public bool IsCrossGroupDrag { get; set; }
    public bool DragStartedFromFolderOverlay { get; set; }
    public IconInfo? DraggedIcon { get; set; }
    public List<IconInfo> DraggedIcons { get; } = new();
    public int DragSourceIndex { get; set; } = -1;
    public bool SingleIconLiveMoveMutatedTiles { get; set; }

    // ── Folder drop state ───────────────────────────────────────────
    public IconInfo? HighlightedFolderDropTarget { get; set; }
    public bool FolderDropCommittedByDropEvent { get; set; }

    // ── Batch drag internals ────────────────────────────────────────
    public bool SyncingPlaceholderBlockMove { get; set; }
    public int LiveReorderSlideGeneration { get; set; }

    // ── Group tab interaction ───────────────────────────────────────
    public bool GroupTabReorderSuppressedForIconDrag { get; set; }

    // ── Already-extracted sub-state instances ────────────────────────
    public LauncherSingleIconDragPreviewState SingleIconPreview { get; } = new();
    public LauncherPointerCacheState PointerCache { get; } = new();
    public LauncherFolderDropIntentState<IconInfo> FolderDropIntent { get; } = new();
    public LauncherAllGroupsOrderState<IconInfo> AllGroupsOrder { get; } = new();
    public LauncherCrossGroupPreviewCoordinator<MenuInfo, WorkspaceNavigationSnapshot, IconInfo>
        CrossGroupPreview { get; } = new(WorkspaceNavigationSnapshot.AllGroups);

    // ── Batch placeholder coordinator (owns its own state) ──────────
    public LauncherBatchPlaceholderCoordinator BatchPlaceholders { get; } = new();
    public LauncherBatchDragSessionState BatchDrag => BatchPlaceholders.State;

    // ── Convenience delegated properties ────────────────────────────
    public bool IsMergedBatchDragActive =>
        BatchDrag.IsSuppressedFromTiles && DraggedIcons.Count > 1;

    public bool IsYieldPlaceholderDragActive =>
        BatchDrag.IsSuppressedFromTiles && BatchDrag.YieldPlaceholders.Count > 0;

    public List<IconInfo> BatchDragYieldPlaceholders => BatchDrag.YieldPlaceholders;

    public int BatchDragInsertIndex
    {
        get => BatchDrag.InsertIndex;
        set => BatchDrag.InsertIndex = value;
    }

    public int BatchDragSourceInsertIndex
    {
        get => BatchDrag.SourceInsertIndex;
        set => BatchDrag.SourceInsertIndex = value;
    }

    public bool IsDraggedIconsSuppressedFromTiles
    {
        get => BatchDrag.IsSuppressedFromTiles;
        set => BatchDrag.IsSuppressedFromTiles = value;
    }

    // ── Lifecycle ───────────────────────────────────────────────────

    /// <summary>Reset all drag state for a new drag session.</summary>
    public void Begin()
    {
        IsCustomDragActive = true;
        IsCrossGroupDrag = false;
        DragStartedFromFolderOverlay = false;
        SingleIconLiveMoveMutatedTiles = false;
        FolderDropCommittedByDropEvent = false;
        HighlightedFolderDropTarget = null;
        SyncingPlaceholderBlockMove = false;
        LiveReorderSlideGeneration = 0;
        GroupTabReorderSuppressedForIconDrag = false;
        SingleIconPreview.ClearPending();
        PointerCache.Clear();
    }

    /// <summary>Reset all state when drag ends.</summary>
    public void Reset()
    {
        IsCustomDragActive = false;
        IsCrossGroupDrag = false;
        DragStartedFromFolderOverlay = false;
        DraggedIcon = null;
        DraggedIcons.Clear();
        DragSourceIndex = -1;
        SingleIconLiveMoveMutatedTiles = false;
        HighlightedFolderDropTarget = null;
        FolderDropCommittedByDropEvent = false;
        SyncingPlaceholderBlockMove = false;
        LiveReorderSlideGeneration = 0;
        GroupTabReorderSuppressedForIconDrag = false;
        SingleIconPreview.ClearPending();
        PointerCache.Clear();
        FolderDropIntent.Reset();
        BatchDrag.Reset();
    }
}
