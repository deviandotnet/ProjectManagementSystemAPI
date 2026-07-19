using Microsoft.EntityFrameworkCore;
using PMS.Domain.Entities;

namespace PMS.Application.Abstractions.Data;

public interface IApplicationDbContext
{

    // ── Core Tables ────────────────────────────────────────────────────────────
    DbSet<Users> Users { get; }
    DbSet<Project> Projects { get; }
    DbSet<ProjectMember> ProjectMembers { get; }

    // ── Categorization ─────────────────────────────────────────────────────────
    DbSet<Category> Categories { get; }
    DbSet<SubCategory> SubCategories { get; }

    // ── Action Items & Scheduling ──────────────────────────────────────────────
    DbSet<ActionItems> ActionItems { get; }
    DbSet<PlannedSchedule> PlannedSchedules { get; }
    DbSet<ActualExecution> ActualExecutions { get; }

    // ── Global / Cross-cutting ─────────────────────────────────────────────────
    DbSet<HolidayCalendar> HolidayCalendar { get; }
    DbSet<AuditLog> AuditLogs { get; }

    // ──────────────────────────────────────────────────────────────────────────
}
