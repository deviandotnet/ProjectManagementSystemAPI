using Microsoft.EntityFrameworkCore;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.AuditLogs;
using PMS.Domain.Categories;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;

namespace PMS.Application.Abstractions.Data;

public interface IApplicationDbContext
{
    // ── Core Tables ────────────────────────────────────────────────────────────
    DbSet<User> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }

    // ── Categorization ─────────────────────────────────────────────────────────
    DbSet<Category> Categories { get; }
    DbSet<SubCategory> SubCategories { get; }

    // ── Action Items & Scheduling ──────────────────────────────────────────────
    DbSet<ActionItem> ActionItems { get; }
    DbSet<PlannedSchedule> PlannedSchedules { get; }
    DbSet<ActualExecution> ActualExecutions { get; }

    // ── Global / Cross-cutting ─────────────────────────────────────────────────
    DbSet<HolidayCalendar> HolidayCalendar { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // ──────────────────────────────────────────────────────────────────────────
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
