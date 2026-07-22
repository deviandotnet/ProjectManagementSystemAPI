using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;

namespace PMS.Application.Features.ProjectFeatures.GetAllProjects;

/// <summary>
/// Handler for retrieving all projects.
/// Uses IApplicationDbContext directly for query flexibility (AsNoTracking, projection).
/// 
/// Request: Unit (no input needed)
/// Response: Result&lt;IEnumerable&lt;GetAllProjectsResponse&gt;&gt;
/// </summary>
public sealed class GetAllProjectsHandler(IApplicationDbContext dbContext)
    : IHandler<Unit, Result<IEnumerable<GetAllProjectsResponse>>>
{
    public async Task<Result<IEnumerable<GetAllProjectsResponse>>> HandleAsync(
        Unit command,
        CancellationToken cancellationToken)
    {
        var projects = await dbContext.Projects
            .AsNoTracking()
            .Select(p => new GetAllProjectsResponse(
                p.Id,
                p.Name,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.WeekStartDay,
                p.DefaultTimelineScale.ToString(),
                p.ProgressMode.ToString(),
                p.Status.ToString(),
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<GetAllProjectsResponse>>(projects);
    }
}

/// <summary>
/// Marker type for handlers that require no input parameters.
/// Used instead of MediatR's Unit — lightweight, no external dependency.
/// </summary>
public readonly record struct Unit;