namespace PMS.Application.Features.ProjectFeatures.GetProjectById;

/// <summary>
/// Response DTO for GetProjectById.
/// Mirrors the full Project entity shape — enums returned as strings for API readability.
/// Includes audit metadata (CreatedByUserId, CreatedAt, UpdatedAt).
/// </summary>
public sealed record GetProjectByIdResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekStartDay,
    string DefaultTimelineScale,
    string ProgressMode,
    string Status,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

/// <summary>
/// Request record for GetProjectById.
/// Contains the ProjectId extracted from the route parameter.
/// </summary>
public sealed record GetProjectByIdRequest(Guid ProjectId);

