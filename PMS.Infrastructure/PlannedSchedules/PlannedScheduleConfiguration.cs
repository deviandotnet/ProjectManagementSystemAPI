using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.ActionItems;
using PMS.Domain.PlannedSchedules;

namespace PMS.Infrastructure.PlannedSchedules;

/// <summary>
/// EF Core fluent configuration for the PlannedSchedule entity.
/// Table: tbl.ps_PlannedSchedules
/// 1:1 with ActionItem. Enforced via unique index on ActionItemId.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class PlannedScheduleConfiguration : IEntityTypeConfiguration<PlannedSchedule>
{
    public void Configure(EntityTypeBuilder<PlannedSchedule> builder)
    {
        builder.ToTable("tbl.ps_PlannedSchedules");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        // Enforce 1:1 — only one PlannedSchedule per ActionItem allowed
        builder.HasIndex(ps => ps.ActionItemId)
            .IsUnique();

        builder.Property(ps => ps.PlannedStartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(ps => ps.PlannedEndDate)
            .HasColumnType("date")
            .IsRequired();

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
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(ps => ps.UpdatedAt);

        // ── Relationships (Shadow-Style / 1:1 Explicit Mapping) ────────────────
        // 1:1 with ActionItem — PlannedSchedule is the dependent side holding ActionItemId
        builder.HasOne<ActionItem>()
            .WithOne()
            .HasForeignKey<PlannedSchedule>(ps => ps.ActionItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
