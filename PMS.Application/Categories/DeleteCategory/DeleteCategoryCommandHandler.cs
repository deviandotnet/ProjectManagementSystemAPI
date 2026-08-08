using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.DeleteCategory;

internal sealed class DeleteCategoryCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<DeleteCategoryCommand>
{
    public async Task<Result> Handle(
        DeleteCategoryCommand command,
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
        // Highest Hierarchy: SystemAdmin OR Project Owner/Admin (UserRole.Admin) can delete ALL categories in the project.
        // Lower Hierarchy: Project members can delete ONLY their OWN created categories.
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

        category.Raise(new CategoryDeletedDomainEvent(category.Id));

        context.Categories.Remove(category);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
