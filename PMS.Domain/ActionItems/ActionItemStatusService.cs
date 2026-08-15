namespace PMS.Domain.ActionItems;

/// <summary>
/// Domain service that computes the real-time runtime status of an action item.
/// Status is strictly calculated at runtime and never persisted directly in the database.
/// </summary>
public static class ActionItemStatusService
{
    public static ActionItemStatus ComputeStatus(
        DateOnly? plannedEndDate,
        DateOnly? actualStartDate,
        DateOnly? actualEndDate,
        DateOnly today)
    {
        if (actualEndDate.HasValue)
        {
            if (!plannedEndDate.HasValue || actualEndDate.Value < plannedEndDate.Value)
                return ActionItemStatus.CompletedEarly;

            if (actualEndDate.Value == plannedEndDate.Value)
                return ActionItemStatus.CompletedOntime;

            return ActionItemStatus.CompletedLate;
        }

        if (actualStartDate.HasValue)
        {
            return ActionItemStatus.Ongoing;
        }

        if (plannedEndDate.HasValue && today > plannedEndDate.Value)
        {
            return ActionItemStatus.Delayed;
        }

        return ActionItemStatus.Plan;
    }
}
