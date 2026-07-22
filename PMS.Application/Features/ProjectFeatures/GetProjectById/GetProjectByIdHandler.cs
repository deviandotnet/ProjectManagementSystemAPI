using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;

namespace PMS.Application.Features.ProjectFeatures.GetProjectById;

/// <summary>
/// Handler for retrieving a single project by its ID.
/// Uses IApplicationDbContext directly for query flexibility (AsNoTracking, projection).
/// 
/// Request: GetProjectByIdRequest (contains ProjectId)
/// Response: Result&lt;GetProjectByIdResponse&gt;
/// </summary>
public sealed class GetProjectByIdHandler(IApplicationDbContext dbContext)
    : IHandler<GetProjectByIdRequest, Result<GetProjectByIdResponse>>
{
    public async Task<Result<GetProjectByIdResponse>> HandleAsync(
        GetProjectByIdRequest command,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(p => p.Id == command.ProjectId)
            .Select(p => new GetProjectByIdResponse(
                p.Id,
                p.Name,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.WeekStartDay,
                p.DefaultTimelineScale.ToString(),
                p.ProgressMode.ToString(),
                p.Status.ToString(),
                p.CreatedByUserId,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return ProjectErrors.NotFound(command.ProjectId);
        }

        return Result.Success(project);
    }
}
