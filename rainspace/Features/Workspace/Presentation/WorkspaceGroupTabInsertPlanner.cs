namespace rainspace.Features.Workspace.Presentation;

internal readonly record struct WorkspaceGroupTabSlot(
    int Index,
    double Top,
    double Height,
    bool IsDragged)
{
    public double Midpoint => Top + Height / 2d;

    public double Bottom => Top + Height;
}

/// <summary>
/// Workspace 分组标签插入位置与相邻滞回的纯规划器；索引 0 固定为”全部”。
/// </summary>
internal static class WorkspaceGroupTabInsertPlanner
{
    public static int Resolve(
        double pointerY,
        int itemCount,
        IReadOnlyList<WorkspaceGroupTabSlot> slots,
        bool isActiveDrag,
        int currentIndex,
        double hysteresis)
    {
        ArgumentNullException.ThrowIfNull(slots);
        if (itemCount <= 1)
        {
            return -1;
        }

        return isActiveDrag && currentIndex >= 1
            ? ResolveDuringDrag(pointerY, itemCount, slots, currentIndex, hysteresis)
            : ResolveInitial(pointerY, itemCount, slots);
    }

    private static int ResolveInitial(
        double pointerY,
        int itemCount,
        IReadOnlyList<WorkspaceGroupTabSlot> slots)
    {
        foreach (WorkspaceGroupTabSlot slot in slots.OrderBy(slot => slot.Index))
        {
            if (slot.IsDragged || slot.Height <= 0d)
            {
                continue;
            }

            if (pointerY < slot.Midpoint)
            {
                return Math.Max(1, slot.Index);
            }
        }

        return itemCount - 1;
    }

    private static int ResolveDuringDrag(
        double pointerY,
        int itemCount,
        IReadOnlyList<WorkspaceGroupTabSlot> slots,
        int currentIndex,
        double hysteresis)
    {
        int candidate = currentIndex;
        double safeHysteresis = Math.Max(0d, hysteresis);
        WorkspaceGroupTabSlot? previous = slots
            .Where(slot => !slot.IsDragged && slot.Index >= 1 && slot.Index < currentIndex)
            .OrderByDescending(slot => slot.Index)
            .Cast<WorkspaceGroupTabSlot?>()
            .FirstOrDefault();
        if (previous.HasValue && pointerY < previous.Value.Bottom + safeHysteresis)
        {
            candidate = previous.Value.Index;
        }

        WorkspaceGroupTabSlot? next = slots
            .Where(slot => !slot.IsDragged && slot.Index > currentIndex)
            .OrderBy(slot => slot.Index)
            .Cast<WorkspaceGroupTabSlot?>()
            .FirstOrDefault();
        if (next.HasValue && pointerY > next.Value.Top - safeHysteresis)
        {
            candidate = next.Value.Index;
        }

        return Math.Clamp(candidate, 1, itemCount - 1);
    }
}
