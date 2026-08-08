using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.SubCategories.GetSubCategoriesByCategoryId;

internal sealed class GetSubCategoriesByCategoryIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetSubCategoriesByCategoryIdQuery, IReadOnlyCollection<SubCategoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<SubCategoryResponse>>> Handle(
        GetSubCategoriesByCategoryIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyCollection<SubCategoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Category? category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<IReadOnlyCollection<SubCategoryResponse>>(CategoryErrors.NotFound(query.CategoryId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<IReadOnlyCollection<SubCategoryResponse>>(SubCategoryErrors.NotProjectMember);
            }
        }

        List<SubCategoryResponse> subCategories = await context.SubCategories
            .AsNoTracking()
            .Where(sc => sc.CategoryId == query.CategoryId)
            .OrderBy(sc => sc.DisplayOrder)
            .Select(sc => new SubCategoryResponse(
                sc.Id,
                sc.CategoryId,
                sc.Name,
                sc.DisplayOrder))
            .ToListAsync(cancellationToken);

        return subCategories;
    }
}
