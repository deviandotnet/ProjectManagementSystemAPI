using PMS.Domain.Enums;

namespace PMS.Application.Features.ProjectFeatures.GetAllProjects;

/// <summary>
/// Response DTO for GetAllProjects.
/// Flattens the Project entity into a transport-safe record — 
/// enums are returned as strings for readability in the API response.
/// </summary>
public sealed record GetAllProjectsResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekStartDay,
    string DefaultTimelineScale,
    string ProgressMode,
    string Status,
    DateTimeOffset CreatedAt
);
