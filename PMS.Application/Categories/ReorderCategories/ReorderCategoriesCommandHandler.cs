using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.ReorderCategories;

internal sealed class ReorderCategoriesCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<ReorderCategoriesCommand>
{
    public async Task<Result> Handle(
        ReorderCategoriesCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

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
        }

        List<Guid> categoryIds = command.Items.Select(i => i.CategoryId).ToList();

        List<Category> categories = await context.Categories
            .Where(c => c.ProjectId == command.ProjectId && categoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> orderMap = command.Items.ToDictionary(i => i.CategoryId, i => i.DisplayOrder);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (Category category in categories)
        {
            if (orderMap.TryGetValue(category.Id, out int newDisplayOrder))
            {
                category.DisplayOrder = newDisplayOrder;
                category.UpdatedAt = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
