using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;
using PMS.Domain.Enums;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the Project entity.
    /// Table: Projects
    /// </summary>
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("tbl.ps_Projects");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Id)
                .HasDefaultValueSql("NEWID()");

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(p => p.Description)
                .HasColumnType("nvarchar(max)");

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

            // Enum stored as tinyint
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
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(p => p.UpdatedAt);

            // ── Relationships ──────────────────────────────────────────────────────
            // One Project → many Categories
            builder.HasMany(p => p.Categories)
                .WithOne(c => c.Project)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Project → many ProjectMembers
            builder.HasMany(p => p.ProjectMembers)
                .WithOne(pm => pm.Project)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Project → many ActionItems
            builder.HasMany(p => p.ActionItems)
                .WithOne(ai => ai.Project)
                .HasForeignKey(ai => ai.ProjectId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict: ActionItems deleted via Category cascade
        }
    }
}
