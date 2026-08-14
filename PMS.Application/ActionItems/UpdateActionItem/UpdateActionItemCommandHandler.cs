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
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.UpdateActionItem;

internal sealed class UpdateActionItemCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateActionItemCommand>
{
    public async Task<Result> Handle(
        UpdateActionItemCommand command,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        // ── 3. Authorization Check (SystemAdmin OR ProjectMember != Viewer)
        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure(ActionItemErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure(ActionItemErrors.ReadOnlyAccess);
            }
        }

        // ── 4. ActionItem Existence Check ─────────────────────────────────
        ActionItem? actionItem = await context.ActionItems
            .SingleOrDefaultAsync(a => a.Id == command.ActionItemId && a.ProjectId == command.ProjectId, cancellationToken);

        if (actionItem is null)
        {
            return Result.Failure(ActionItemErrors.NotFound(command.ActionItemId));
        }

        // ── 5. Category & SubCategory Validation ───────────────────────────
        bool categoryExists = await context.Categories
            .AnyAsync(c => c.Id == command.CategoryId && c.ProjectId == command.ProjectId, cancellationToken);

        if (!categoryExists)
        {
            return Result.Failure(CategoryErrors.NotFound(command.CategoryId));
        }

        if (command.SubCategoryId.HasValue)
        {
            bool subCategoryExists = await context.SubCategories
                .AnyAsync(sc => sc.Id == command.SubCategoryId.Value && sc.CategoryId == command.CategoryId, cancellationToken);

            if (!subCategoryExists)
            {
                return Result.Failure(Error.NotFound(
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

        int durationWorkingDays = CalculateWorkingDays(command.PlannedStartDate, command.PlannedEndDate, new HashSet<DateOnly>(holidayDates));

        // ── 7. Mutate ActionItem Aggregate ────────────────────────────────
        actionItem.CategoryId = command.CategoryId;
        actionItem.SubCategoryId = command.SubCategoryId;
        actionItem.ActionItemName = command.ActionItemName;
        actionItem.Description = command.Description;
        actionItem.Priority = command.Priority;
        actionItem.OwnerName = command.OwnerName;
        actionItem.OwnerId = command.OwnerId;
        actionItem.Weight = command.Weight;
        actionItem.Sequence = command.Sequence;
        actionItem.Remarks = command.Remarks;
        actionItem.UpdatedByUserId = userId;
        actionItem.UpdatedAt = dateTimeProvider.UtcNow;

        PlannedSchedule? schedule = await context.PlannedSchedules
            .SingleOrDefaultAsync(s => s.ActionItemId == actionItem.Id, cancellationToken);

        if (schedule is null)
        {
            schedule = new PlannedSchedule
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItem.Id,
                PlannedStartDate = command.PlannedStartDate,
                PlannedEndDate = command.PlannedEndDate,
                PlannedStartWeek = plannedStartWeek,
                PlannedEndWeek = plannedEndWeek,
                DurationCalendarDays = durationCalendarDays,
                DurationWorkingDays = durationWorkingDays,
                CreatedAt = dateTimeProvider.UtcNow
            };
            context.PlannedSchedules.Add(schedule);
        }
        else
        {
            schedule.PlannedStartDate = command.PlannedStartDate;
            schedule.PlannedEndDate = command.PlannedEndDate;
            schedule.PlannedStartWeek = plannedStartWeek;
            schedule.PlannedEndWeek = plannedEndWeek;
            schedule.DurationCalendarDays = durationCalendarDays;
            schedule.DurationWorkingDays = durationWorkingDays;
            schedule.UpdatedAt = dateTimeProvider.UtcNow;
        }

        ActualExecution? execution = await context.ActualExecutions
            .SingleOrDefaultAsync(a => a.ActionItemId == actionItem.Id, cancellationToken);

        if (command.ActualStartDate.HasValue || command.ActualEndDate.HasValue || command.ActualHours.HasValue || command.DelayReason != null)
        {
            if (execution is null)
            {
                execution = new ActualExecution
                {
                    Id = Guid.NewGuid(),
                    ActionItemId = actionItem.Id,
                    ActualStartDate = command.ActualStartDate,
                    ActualEndDate = command.ActualEndDate,
                    ActualHours = command.ActualHours,
                    CompletedByName = command.ActualEndDate.HasValue ? command.OwnerName : null,
                    CompletedById = command.ActualEndDate.HasValue ? command.OwnerId : null,
                    DelayReason = command.DelayReason,
                    CreatedAt = dateTimeProvider.UtcNow
                };
                context.ActualExecutions.Add(execution);
            }
            else
            {
                execution.ActualStartDate = command.ActualStartDate;
                execution.ActualEndDate = command.ActualEndDate;
                execution.ActualHours = command.ActualHours;
                execution.CompletedByName = command.ActualEndDate.HasValue ? command.OwnerName : execution.CompletedByName;
                execution.CompletedById = command.ActualEndDate.HasValue ? command.OwnerId : execution.CompletedById;
                execution.DelayReason = command.DelayReason;
                execution.UpdatedAt = dateTimeProvider.UtcNow;
            }
        }

        // ── 8. Raise Domain Event & Save ───────────────────────────────────
        actionItem.Raise(new ActionItemUpdatedDomainEvent(actionItem.Id, actionItem.ProjectId));

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
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
}
