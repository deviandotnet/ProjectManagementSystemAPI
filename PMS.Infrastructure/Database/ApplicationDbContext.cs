using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Data;
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

namespace PMS.Infrastructure.Database
{
    /// <summary>
    /// EF Core DbContext for the Project Management System.
    /// 
    /// Design rules (for AI agents and developers):
    ///   1. No fluent configuration lives inline here — all configuration is delegated
    ///      to IEntityTypeConfiguration&lt;T&gt; classes inside the Configurations/ folder.
    ///   2. Every domain entity that maps to a database table has exactly one DbSet here.
    ///   3. ApplyConfigurationsFromAssembly() automatically discovers all IEntityTypeConfiguration
    ///      classes in this assembly — no manual registration needed when you add a new one.
    /// </summary>
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ── Core Tables ────────────────────────────────────────────────────────────
        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();

        // ── Categorization ─────────────────────────────────────────────────────────
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<SubCategory> SubCategories => Set<SubCategory>();

        // ── Action Items & Scheduling ──────────────────────────────────────────────
        public DbSet<ActionItem> ActionItems => Set<ActionItem>();
        public DbSet<PlannedSchedule> PlannedSchedules => Set<PlannedSchedule>();
        public DbSet<ActualExecution> ActualExecutions => Set<ActualExecution>();

        // ── Global / Cross-cutting ─────────────────────────────────────────────────
        public DbSet<HolidayCalendar> HolidayCalendar => Set<HolidayCalendar>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // ──────────────────────────────────────────────────────────────────────────

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Automatically picks up every IEntityTypeConfiguration<T> class
            // in this assembly. Adding a new config file is all that's needed.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            base.OnModelCreating(modelBuilder);
        }

    }
}
