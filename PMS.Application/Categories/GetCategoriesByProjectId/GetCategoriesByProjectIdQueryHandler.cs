using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
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
    : IQueryHandler<GetCategoriesByProjectIdQuery, PagedResponse<CategoryResponse>>
{
    public async Task<Result<PagedResponse<CategoryResponse>>> Handle(
        GetCategoriesByProjectIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<CategoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<PagedResponse<CategoryResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedResponse<CategoryResponse>>(CategoryErrors.NotProjectMember);
            }
        }

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var categoriesQuery = context.Categories
            .AsNoTracking()
            .Where(c => c.ProjectId == query.ProjectId);

        int totalCount = await categoriesQuery.CountAsync(cancellationToken);

        List<CategoryResponse> categories = await categoriesQuery
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(c => new CategoryResponse(
                c.Id,
                c.ProjectId,
                c.Name,
                c.DisplayOrder,
                c.Color,
                c.CreatedByUserId))
            .ToListAsync(cancellationToken);

        return PagedResponse<CategoryResponse>.Create(
            categories,
            pageNumber,
            pageSize,
            totalCount);
    }
}
