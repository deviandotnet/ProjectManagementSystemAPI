using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.SubCategories.GetSubCategoriesByCategoryId;

internal sealed class GetSubCategoriesByCategoryIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IApplicationCache? cache = null)
    : IQueryHandler<GetSubCategoriesByCategoryIdQuery, PagedResponse<SubCategoryResponse>>
{
    public async Task<Result<PagedResponse<SubCategoryResponse>>> Handle(
        GetSubCategoriesByCategoryIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<SubCategoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Category? category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<PagedResponse<SubCategoryResponse>>(CategoryErrors.NotFound(query.CategoryId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedResponse<SubCategoryResponse>>(SubCategoryErrors.NotProjectMember);
            }
        }

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        IApplicationCache applicationCache = cache ?? NullApplicationCache.Instance;
        string key = CacheKeyBuilder.Create("category-subcategories", query.CategoryId, pageNumber, pageSize);

        return await applicationCache.GetOrCreateAsync(
            key,
            token => LoadPageAsync(query.CategoryId, pageNumber, pageSize, skip, token),
            CachePolicy.Collection,
            [CacheTags.Project(category.ProjectId), CacheTags.CategorySubCategories(query.CategoryId)],
            cancellationToken);
    }

    private async ValueTask<PagedResponse<SubCategoryResponse>> LoadPageAsync(
        Guid categoryId,
        int pageNumber,
        int pageSize,
        int skip,
        CancellationToken cancellationToken)
    {

        var subCategoriesQuery = context.SubCategories
            .AsNoTracking()
            .Where(sc => sc.CategoryId == categoryId);

        int totalCount = await subCategoriesQuery.CountAsync(cancellationToken);

        List<SubCategoryResponse> subCategories = await subCategoriesQuery
            .OrderBy(sc => sc.DisplayOrder)
            .ThenBy(sc => sc.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(sc => new SubCategoryResponse(
                sc.Id,
                sc.CategoryId,
                sc.Name,
                sc.DisplayOrder))
            .ToListAsync(cancellationToken);

        return PagedResponse<SubCategoryResponse>.Create(
            subCategories,
            pageNumber,
            pageSize,
            totalCount);
    }
}
