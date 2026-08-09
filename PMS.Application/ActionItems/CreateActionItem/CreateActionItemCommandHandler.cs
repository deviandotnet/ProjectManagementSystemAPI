using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.CreateActionItem;

internal sealed class CreateActionItemCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<CreateActionItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateActionItemCommand command,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<Guid>(ProjectErrors.NotFound(command.ProjectId));
        }

        // ── 3. Authorization Check (SystemAdmin OR ProjectMember != Viewer)
        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure<Guid>(ActionItemErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure<Guid>(ActionItemErrors.ReadOnlyAccess);
            }
        }

        // ── 4. Category Validation ─────────────────────────────────────────
        bool categoryExists = await context.Categories
            .AnyAsync(c => c.Id == command.CategoryId && c.ProjectId == command.ProjectId, cancellationToken);

        if (!categoryExists)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound(command.CategoryId));
        }

        // ── 5. SubCategory Validation ──────────────────────────────────────
        if (command.SubCategoryId.HasValue)
        {
            bool subCategoryExists = await context.SubCategories
                .AnyAsync(sc => sc.Id == command.SubCategoryId.Value && sc.CategoryId == command.CategoryId, cancellationToken);

            if (!subCategoryExists)
            {
                return Result.Failure<Guid>(Error.NotFound(
                    "SubCategories.NotFound",
                    $"SubCategory '{command.SubCategoryId.Value}' was not found under Category '{command.CategoryId}'."));
            }
        }

        // ── 6. Schedule Calculations ───────────────────────────────────────
        string plannedStartWeek = GetWeekLabel(command.PlannedStartDate);
        string plannedEndWeek = GetWeekLabel(command.PlannedEndDate);

        int durationCalendarDays = (command.PlannedEndDate.DayNumber - command.PlannedStartDate.DayNumber) + 1;

        List<DateOnly> holidayDates = await context.HolidayCalendar
            .AsNoTracking()
            .Select(h => h.HolidayDate)
            .ToListAsync(cancellationToken);

        int durationWorkingDays = CalculateWorkingDays(command.PlannedStartDate, command.PlannedEndDate, holidayDates);

        // ── 7. Construct ActionItem Aggregate ─────────────────────────────
        var actionItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            CategoryId = command.CategoryId,
            SubCategoryId = command.SubCategoryId,
            ActionItemName = command.ActionItemName,
            Description = command.Description,
            Priority = command.Priority,
            OwnerName = command.OwnerName,
            OwnerId = command.OwnerId,
            Weight = command.Weight,
            Sequence = command.Sequence,
            Remarks = command.Remarks,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var plannedSchedule = new PlannedSchedule
        {
            Id = Guid.NewGuid(),
            ActionItemId = actionItem.Id,
            PlannedStartDate = command.PlannedStartDate,
            PlannedEndDate = command.PlannedEndDate,
            PlannedStartWeek = plannedStartWeek,
            PlannedEndWeek = plannedEndWeek,
            DurationCalendarDays = durationCalendarDays,
            DurationWorkingDays = durationWorkingDays,
            CreatedAt = DateTimeOffset.UtcNow
        };

        ActualExecution? actualExecution = null;
        if (command.ActualStartDate.HasValue || command.ActualEndDate.HasValue || command.ActualHours.HasValue || command.DelayReason != null)
        {
            actualExecution = new ActualExecution
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItem.Id,
                ActualStartDate = command.ActualStartDate,
                ActualEndDate = command.ActualEndDate,
                ActualHours = command.ActualHours,
                CompletedByName = command.ActualEndDate.HasValue ? command.OwnerName : null,
                CompletedById = command.ActualEndDate.HasValue ? command.OwnerId : null,
                DelayReason = command.DelayReason,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        // ── 8. Raise Domain Event & Save ───────────────────────────────────
        actionItem.Raise(new ActionItemCreatedDomainEvent(actionItem.Id, actionItem.ProjectId));

        context.ActionItems.Add(actionItem);
        context.PlannedSchedules.Add(plannedSchedule);

        if (actualExecution is not null)
        {
            context.ActualExecutions.Add(actualExecution);
        }

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return actionItem.Id;
    }

    private static string GetWeekLabel(DateOnly date)
    {
        int weekNumber = ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
        return $"WW{weekNumber:D2}";
    }

    private static int CalculateWorkingDays(DateOnly startDate, DateOnly endDate, HashSet<DateOnly> holidayDates)
    {
        int workingDays = 0;
        for (DateOnly date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday &&
                !holidayDates.Contains(date))
            {
                workingDays++;
            }
        }
        return workingDays;
    }

    private static int CalculateWorkingDays(DateOnly startDate, DateOnly endDate, List<DateOnly> holidayDates)
    {
        return CalculateWorkingDays(startDate, endDate, new HashSet<DateOnly>(holidayDates));
    }
}
