using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.UpdateCategory;

internal sealed class UpdateCategoryCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateCategoryCommand>
{
    public async Task<Result> Handle(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Category? category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId && c.ProjectId == command.ProjectId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(CategoryErrors.NotFound(command.CategoryId));
        }

        // Authorization Hierarchy Check:
        // Highest Hierarchy: SystemAdmin OR Project Owner/Admin (UserRole.Admin) can edit ALL categories in the project.
        // Lower Hierarchy: Project members can edit ONLY their OWN created categories.
        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure(CategoryErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure(CategoryErrors.ReadOnlyAccess);
            }

            bool isProjectOwnerOrAdmin = member.Role == UserRole.Admin;
            bool isCreator = category.CreatedByUserId.HasValue && category.CreatedByUserId.Value == userId;

            if (!isProjectOwnerOrAdmin && !isCreator)
            {
                return Result.Failure(CategoryErrors.Forbidden);
            }
        }

        category.Name = command.Name;
        category.DisplayOrder = command.DisplayOrder;
        category.Color = command.Color;
        category.UpdatedByUserId = userId;
        category.UpdatedAt = dateTimeProvider.UtcNow;

        category.Raise(new CategoryUpdatedDomainEvent(category.Id));

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
