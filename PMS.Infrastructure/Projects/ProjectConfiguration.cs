using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Infrastructure.Projects;

/// <summary>
/// EF Core fluent configuration for the Project entity.
/// Table: tbl.ps_Projects
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("tbl.ps_Projects");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasColumnType("text");

        // DateOnly maps to SQL DATE — no time portion stored
        builder.Property(p => p.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(p => p.EndDate)
            .HasColumnType("date")
            .IsRequired();

        // WeekStartDay: 0=Sunday … 6=Saturday. Default = 1 (Monday).
        builder.Property(p => p.WeekStartDay)
            .IsRequired()
            .HasDefaultValue(1);

        // Enum stored as tinyint/byte
        builder.Property(p => p.DefaultTimelineScale)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(TimelineScale.Weekly);

        builder.Property(p => p.ProgressMode)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(ProgressMode.CountBased);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<byte>()
            .HasDefaultValue(ProjectStatus.Active);

        // ── Audit columns ──────────────────────────────────────────────────────
        builder.Property(p => p.CreatedByUserId)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(p => p.UpdatedAt);

        // ── Relationships (Shadow-Style) ───────────────────────────────────────
        // Project references User creator/owner via foreign-key id CreatedByUserId
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
