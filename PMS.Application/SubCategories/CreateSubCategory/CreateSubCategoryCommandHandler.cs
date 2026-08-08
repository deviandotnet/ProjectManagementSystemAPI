using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.SubCategories.CreateSubCategory;

internal sealed class CreateSubCategoryCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateSubCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateSubCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Category? category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(command.CategoryId));
        }

        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure<Guid>(SubCategoryErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure<Guid>(SubCategoryErrors.ReadOnlyAccess);
            }
        }

        var subCategory = new SubCategory
        {
            Id = Guid.NewGuid(),
            CategoryId = command.CategoryId,
            Name = command.Name.Trim(),
            DisplayOrder = command.DisplayOrder,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        subCategory.Raise(new SubCategoryCreatedDomainEvent(subCategory.Id));

        context.SubCategories.Add(subCategory);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return subCategory.Id;
    }
}
