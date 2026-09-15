using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Categories.GetCategoriesWithActionItems;

internal sealed class GetCategoriesWithActionItemsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    IApplicationCache? cache = null)
    : IQueryHandler<GetCategoriesWithActionItemsQuery, CategoriesWithActionItemsResponse>
{
    public async Task<Result<CategoriesWithActionItemsResponse>> Handle(
        GetCategoriesWithActionItemsQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<CategoriesWithActionItemsResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<CategoriesWithActionItemsResponse>(
                ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(
                    member => member.ProjectId == query.ProjectId && member.UserId == userId,
                    cancellationToken);

            if (!isMember)
            {
                return Result.Failure<CategoriesWithActionItemsResponse>(
                    CategoryErrors.NotProjectMember);
            }
        }

        int pageNumber = CacheKeyBuilder.NormalizePageNumber(query.PageNumber);
        int pageSize = CacheKeyBuilder.NormalizePageSize(query.PageSize);
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        string filterFingerprint = CacheKeyBuilder.Fingerprint(
            query.CategoryId,
            query.SubCategoryId,
            query.Statuses ?? [],
            query.Priority,
            query.OwnerName,
            query.Search,
            query.WeekStart,
            query.WeekEnd,
            query.StartDate,
            query.EndDate);

        IApplicationCache applicationCache = cache ?? NullApplicationCache.Instance;
        string key = CacheKeyBuilder.Create(
            "categories-with-action-items",
            query.ProjectId,
            pageNumber,
            pageSize,
            today,
            filterFingerprint);

        return await applicationCache.GetOrCreateAsync(
            key,
            token => LoadPageAsync(query, today, pageNumber, pageSize, token),
            CachePolicy.Computed,
            [
                CacheTags.Project(query.ProjectId),
                CacheTags.ProjectCategories(query.ProjectId),
                CacheTags.ProjectActionItems(query.ProjectId)
            ],
            cancellationToken);
    }

    private async ValueTask<CategoriesWithActionItemsResponse> LoadPageAsync(
        GetCategoriesWithActionItemsQuery query,
        DateOnly today,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var categoryQuery = context.Categories
            .AsNoTracking()
            .Where(category => category.ProjectId == query.ProjectId);

        if (query.CategoryId.HasValue)
        {
            categoryQuery = categoryQuery.Where(
                category => category.Id == query.CategoryId.Value);
        }

        List<CategoryReadModel> categoryRows = await categoryQuery
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryReadModel(
                category.Id,
                category.ProjectId,
                category.Name,
                category.DisplayOrder,
                category.Color))
            .ToListAsync(cancellationToken);

        var dbQuery =
            from actionItem in context.ActionItems.AsNoTracking()
            join category in context.Categories.AsNoTracking()
                on actionItem.CategoryId equals category.Id
            join subCategory in context.SubCategories.AsNoTracking()
                on actionItem.SubCategoryId equals subCategory.Id into subCategories
            from subCategory in subCategories.DefaultIfEmpty()
            join plannedSchedule in context.PlannedSchedules.AsNoTracking()
                on actionItem.Id equals plannedSchedule.ActionItemId into plannedSchedules
            from plannedSchedule in plannedSchedules.DefaultIfEmpty()
            join actualExecution in context.ActualExecutions.AsNoTracking()
                on actionItem.Id equals actualExecution.ActionItemId into actualExecutions
            from actualExecution in actualExecutions.DefaultIfEmpty()
            where actionItem.ProjectId == query.ProjectId
            select new
            {
                ActionItem = actionItem,
                Category = category,
                SubCategory = subCategory,
                PlannedSchedule = plannedSchedule,
                ActualExecution = actualExecution
            };

        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.ActionItem.CategoryId == query.CategoryId.Value);
        }

        if (query.SubCategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.ActionItem.SubCategoryId == query.SubCategoryId.Value);
        }

        if (query.Priority.HasValue)
        {
            dbQuery = dbQuery.Where(item => (int)item.ActionItem.Priority == query.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.OwnerName))
        {
            dbQuery = dbQuery.Where(item =>
                item.ActionItem.OwnerName != null &&
                item.ActionItem.OwnerName.Contains(query.OwnerName));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search;
            dbQuery = dbQuery.Where(item =>
                item.ActionItem.ActionItemName.Contains(search) ||
                (item.ActionItem.Description != null &&
                 item.ActionItem.Description.Contains(search)) ||
                (item.ActionItem.OwnerName != null &&
                 item.ActionItem.OwnerName.Contains(search)));
        }

        if (query.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(item =>
                item.PlannedSchedule != null &&
                item.PlannedSchedule.PlannedStartDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(item =>
                item.PlannedSchedule != null &&
                item.PlannedSchedule.PlannedEndDate <= query.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.WeekStart))
        {
            string weekStart = query.WeekStart;
            dbQuery = dbQuery.Where(item =>
                item.PlannedSchedule != null &&
                string.Compare(item.PlannedSchedule.PlannedStartWeek, weekStart) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.WeekEnd))
        {
            string weekEnd = query.WeekEnd;
            dbQuery = dbQuery.Where(item =>
                item.PlannedSchedule != null &&
                string.Compare(item.PlannedSchedule.PlannedEndWeek, weekEnd) <= 0);
        }

        if (query.Statuses is { Length: > 0 })
        {
            bool includePlan = query.Statuses.Contains((int)ActionItemStatus.Plan);
            bool includeOngoing = query.Statuses.Contains((int)ActionItemStatus.Ongoing);
            bool includeDelayed = query.Statuses.Contains((int)ActionItemStatus.Delayed);
            bool includeCompletedEarly =
                query.Statuses.Contains((int)ActionItemStatus.CompletedEarly);
            bool includeCompletedOntime =
                query.Statuses.Contains((int)ActionItemStatus.CompletedOntime);
            bool includeCompletedLate =
                query.Statuses.Contains((int)ActionItemStatus.CompletedLate);

            dbQuery = dbQuery.Where(item =>
                (includePlan &&
                    (item.PlannedSchedule == null ||
                     (item.ActualExecution == null ||
                      (!item.ActualExecution.ActualStartDate.HasValue &&
                       !item.ActualExecution.ActualEndDate.HasValue)) &&
                     today <= item.PlannedSchedule.PlannedEndDate)) ||
                (includeOngoing &&
                    item.PlannedSchedule != null &&
                    item.ActualExecution != null &&
                    item.ActualExecution.ActualStartDate.HasValue &&
                    !item.ActualExecution.ActualEndDate.HasValue) ||
                (includeDelayed &&
                    item.PlannedSchedule != null &&
                    (item.ActualExecution == null ||
                     (!item.ActualExecution.ActualStartDate.HasValue &&
                      !item.ActualExecution.ActualEndDate.HasValue)) &&
                    today > item.PlannedSchedule.PlannedEndDate) ||
                (includeCompletedEarly &&
                    item.PlannedSchedule != null &&
                    item.ActualExecution != null &&
                    item.ActualExecution.ActualEndDate.HasValue &&
                    item.ActualExecution.ActualEndDate <
                    item.PlannedSchedule.PlannedEndDate) ||
                (includeCompletedOntime &&
                    item.PlannedSchedule != null &&
                    item.ActualExecution != null &&
                    item.ActualExecution.ActualEndDate.HasValue &&
                    item.ActualExecution.ActualEndDate ==
                    item.PlannedSchedule.PlannedEndDate) ||
                (includeCompletedLate &&
                    item.PlannedSchedule != null &&
                    item.ActualExecution != null &&
                    item.ActualExecution.ActualEndDate.HasValue &&
                    item.ActualExecution.ActualEndDate >
                    item.PlannedSchedule.PlannedEndDate));
        }

        int totalCount = await dbQuery.CountAsync(cancellationToken);
        int itemsToSkip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        List<CategorizedActionItemReadModel> rawItems = await dbQuery
            .OrderBy(item => item.Category.DisplayOrder)
            .ThenBy(item => item.Category.Id)
            .ThenBy(item => item.ActionItem.SubCategoryId.HasValue)
            .ThenBy(item => item.SubCategory == null ? 0 : item.SubCategory.DisplayOrder)
            .ThenBy(item => item.ActionItem.SubCategoryId)
            .ThenBy(item => item.ActionItem.Sequence)
            .ThenBy(item => item.ActionItem.Id)
            .Skip(itemsToSkip)
            .Take(pageSize)
            .Select(item => new CategorizedActionItemReadModel(
                item.ActionItem.Id,
                item.ActionItem.ActionItemName,
                item.ActionItem.CategoryId,
                item.Category.ProjectId,
                item.Category.Name,
                item.Category.DisplayOrder,
                item.Category.Color,
                item.ActionItem.SubCategoryId,
                item.SubCategory == null ? null : item.SubCategory.Name,
                (int)item.ActionItem.Priority,
                item.ActionItem.OwnerName,
                item.ActionItem.Sequence,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.Id,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.PlannedStartDate,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.PlannedEndDate,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.PlannedStartWeek,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.PlannedEndWeek,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.DurationCalendarDays,
                item.PlannedSchedule == null ? null : item.PlannedSchedule.DurationWorkingDays,
                item.ActualExecution == null ? null : item.ActualExecution.Id,
                item.ActualExecution == null ? null : item.ActualExecution.ActualStartDate,
                item.ActualExecution == null ? null : item.ActualExecution.ActualEndDate,
                item.ActualExecution == null ? null : item.ActualExecution.ActualHours,
                item.ActualExecution == null ? null : item.ActualExecution.DelayReason,
                item.ActionItem.Weight,
                item.ActionItem.Remarks))
            .ToListAsync(cancellationToken);

        ILookup<Guid, CategorizedActionItemReadModel> itemsByCategory =
            rawItems.ToLookup(item => item.CategoryId);

        List<CategoryWithActionItemsResponse> categories = categoryRows
            .Select(category => new CategoryWithActionItemsResponse(
                category.Id,
                category.ProjectId,
                category.Name,
                category.DisplayOrder,
                category.Color,
                itemsByCategory[category.Id]
                    .Select(item => MapActionItem(item, today))
                    .ToList()))
            .ToList();

        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new CategoriesWithActionItemsResponse(
            categories,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
    }

    private sealed record CategoryReadModel(
        Guid Id,
        Guid ProjectId,
        string Name,
        int DisplayOrder,
        string? Color);

    private static CategorizedActionItemResponse MapActionItem(
        CategorizedActionItemReadModel item,
        DateOnly today)
    {
        ActionItemStatus status = ActionItemStatusService.ComputeStatus(
            item.PlannedEndDate,
            item.ActualStartDate,
            item.ActualEndDate,
            today);

        return new CategorizedActionItemResponse(
            item.Id,
            item.ActionItemName,
            item.CategoryId,
            item.CategoryName,
            item.SubCategoryId,
            item.SubCategoryName,
            item.Priority,
            item.OwnerName,
            item.Sequence,
            item.PlannedScheduleId is null
                ? null
                : new CategorizedPlannedScheduleResponse(
                    item.PlannedScheduleId.Value,
                    item.PlannedStartDate!.Value,
                    item.PlannedEndDate!.Value,
                    item.PlannedStartWeek!,
                    item.PlannedEndWeek!,
                    item.DurationCalendarDays!.Value,
                    item.DurationWorkingDays!.Value),
            item.ActualExecutionId is null
                ? null
                : new CategorizedActualExecutionResponse(
                    item.ActualExecutionId.Value,
                    item.ActualStartDate,
                    item.ActualEndDate,
                    item.ActualHours,
                    item.DelayReason),
            (int)status,
            status.ToString(),
            item.Weight,
            item.Remarks);
    }

    private sealed record CategorizedActionItemReadModel(
        Guid Id,
        string ActionItemName,
        Guid CategoryId,
        Guid ProjectId,
        string CategoryName,
        int CategoryDisplayOrder,
        string? CategoryColor,
        Guid? SubCategoryId,
        string? SubCategoryName,
        int Priority,
        string? OwnerName,
        int Sequence,
        Guid? PlannedScheduleId,
        DateOnly? PlannedStartDate,
        DateOnly? PlannedEndDate,
        string? PlannedStartWeek,
        string? PlannedEndWeek,
        int? DurationCalendarDays,
        int? DurationWorkingDays,
        Guid? ActualExecutionId,
        DateOnly? ActualStartDate,
        DateOnly? ActualEndDate,
        decimal? ActualHours,
        string? DelayReason,
        decimal? Weight,
        string? Remarks);
}
