using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.CreateCategory;

internal sealed class CreateCategoryCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // Check project existence
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<Guid>(ProjectErrors.NotFound(command.ProjectId));
        }

        // Authorization check: Must be SystemAdmin OR a ProjectMember with Role != Viewer
        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure<Guid>(CategoryErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure<Guid>(CategoryErrors.ReadOnlyAccess);
            }
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            Name = command.Name,
            DisplayOrder = command.DisplayOrder,
            Color = command.Color,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        category.Raise(new CategoryCreatedDomainEvent(category.Id));

        context.Categories.Add(category);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return category.Id;
    }
}
