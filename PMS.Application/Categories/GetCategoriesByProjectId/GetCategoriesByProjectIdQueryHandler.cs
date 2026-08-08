using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.GetCategoriesByProjectId;

internal sealed class GetCategoriesByProjectIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetCategoriesByProjectIdQuery, IReadOnlyCollection<CategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<CategoryResponse>>> Handle(
        GetCategoriesByProjectIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyCollection<CategoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<IReadOnlyCollection<CategoryResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<IReadOnlyCollection<CategoryResponse>>(CategoryErrors.NotProjectMember);
            }
        }

        List<CategoryResponse> categories = await context.Categories
            .AsNoTracking()
            .Where(c => c.ProjectId == query.ProjectId)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryResponse(
                c.Id,
                c.ProjectId,
                c.Name,
                c.DisplayOrder,
                c.Color,
                c.CreatedByUserId))
            .ToListAsync(cancellationToken);

        return categories;
    }
}
