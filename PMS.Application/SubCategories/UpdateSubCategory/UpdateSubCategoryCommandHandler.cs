using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.SubCategories.UpdateSubCategory;

internal sealed class UpdateSubCategoryCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateSubCategoryCommand>
{
    public async Task<Result> Handle(
        UpdateSubCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        SubCategory? subCategory = await context.SubCategories
            .FirstOrDefaultAsync(sc => sc.Id == command.SubCategoryId && sc.CategoryId == command.CategoryId, cancellationToken);

        if (subCategory is null)
        {
            return Result.Failure(SubCategoryErrors.NotFound(command.SubCategoryId));
        }

        Category? category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound(command.CategoryId));
        }

        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure(SubCategoryErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure(SubCategoryErrors.ReadOnlyAccess);
            }

            bool isProjectOwnerOrAdmin = member.Role <= UserRole.ProjectManager;
            bool isCreator = subCategory.CreatedByUserId.HasValue && subCategory.CreatedByUserId.Value == userId;

            if (!isProjectOwnerOrAdmin && !isCreator)
            {
                return Result.Failure(SubCategoryErrors.Forbidden);
            }
        }

        subCategory.Name = command.Name.Trim();
        subCategory.DisplayOrder = command.DisplayOrder;
        subCategory.UpdatedByUserId = userId;
        subCategory.UpdatedAt = DateTimeOffset.UtcNow;

        subCategory.Raise(new SubCategoryUpdatedDomainEvent(subCategory.Id));

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
