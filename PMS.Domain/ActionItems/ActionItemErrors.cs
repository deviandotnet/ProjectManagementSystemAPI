using PMS.SharedKernel;

namespace PMS.Domain.ActionItems;

public static class ActionItemErrors
{
    public static Error NotFound(Guid actionItemId) => Error.NotFound(
        "ActionItems.NotFound",
        $"The action item with Id '{actionItemId}' was not found.");

    public static readonly Error NotProjectMember = Error.Failure(
        "ActionItems.NotProjectMember",
        "You are not a member of this project.");

    public static readonly Error Forbidden = Error.Failure(
        "ActionItems.Forbidden",
        "You do not have permission to perform this action.");

    public static readonly Error ReadOnlyAccess = Error.Failure(
        "ActionItems.ReadOnlyAccess",
        "You have read-only access and cannot modify action items.");
}
