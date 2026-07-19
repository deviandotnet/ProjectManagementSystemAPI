using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the PlannedSchedule entity.
    /// Table: PlannedSchedules
    /// 1:1 with ActionItems. Enforced via unique index on ActionItemId.
    /// All date columns map to SQL DATE (DateOnly in C#) — no time component stored.
    /// PlannedStartWeek / PlannedEndWeek are computed by the Application layer before save.
    /// </summary>
    public class PlannedScheduleConfiguration : IEntityTypeConfiguration<PlannedSchedule>
    {
        public void Configure(EntityTypeBuilder<PlannedSchedule> builder)
        {
            builder.ToTable("tbl.ps_PlannedSchedules");

            builder.HasKey(ps => ps.Id);

            builder.Property(ps => ps.Id)
                .HasDefaultValueSql("NEWID()");

            // Enforce 1:1 — only one PlannedSchedule per ActionItem allowed
            builder.HasIndex(ps => ps.ActionItemId)
                .IsUnique();

            // DateOnly → SQL DATE (no time portion)
            builder.Property(ps => ps.PlannedStartDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(ps => ps.PlannedEndDate)
                .HasColumnType("date")
                .IsRequired();

            // Week label e.g. "WW03" — computed before save, max 5 chars
            builder.Property(ps => ps.PlannedStartWeek)
                .HasMaxLength(5)
                .IsRequired();

            builder.Property(ps => ps.PlannedEndWeek)
                .HasMaxLength(5)
                .IsRequired();

            builder.Property(ps => ps.DurationCalendarDays)
                .IsRequired();

            builder.Property(ps => ps.DurationWorkingDays)
                .IsRequired();

            builder.Property(ps => ps.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(ps => ps.UpdatedAt);

            // ── Relationship ───────────────────────────────────────────────────────
            // 1:1 back-reference — principal side configured in ActionItemConfiguration
            builder.HasOne(ps => ps.ActionItem)
                .WithOne(ai => ai.PlannedSchedule)
                .HasForeignKey<PlannedSchedule>(ps => ps.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
