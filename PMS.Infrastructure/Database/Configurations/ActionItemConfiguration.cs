using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.PlannedSchedules;

namespace PMS.Infrastructure.Database.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the ActionItems entity.
    /// Table: ActionItems
    /// Core entity — every row in the timeline grid is one ActionItem.
    /// ComputedStatus is NEVER stored; it is derived at runtime by StatusEngine.
    /// </summary>
    public class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
    {
        public void Configure(EntityTypeBuilder<ActionItem> builder)
        {
            builder.ToTable("tbl.ps_ActionItems");

            builder.HasKey(ai => ai.Id);

            builder.Property(ai => ai.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(ai => ai.ActionItemName)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(ai => ai.Description)
                .HasMaxLength(500);

            builder.Property(ai => ai.Priority)
                .IsRequired()
                .HasConversion<byte>()
                .HasDefaultValue(Priority.Medium);

            builder.Property(ai => ai.OwnerName)
                .HasMaxLength(200);

            // Weight: used when Project.ProgressMode = WeightBased. Range 0–100.
            builder.Property(ai => ai.Weight)
                .HasColumnType("decimal(5,2)");

            builder.Property(ai => ai.Sequence)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(ai => ai.Remarks)
                .HasColumnType("text");

            // ── Audit columns ──────────────────────────────────────────────────────
            builder.Property(ai => ai.CreatedByUserId)
                .IsRequired(false); // Nullable — may be set by system/seeder

            builder.Property(ai => ai.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");

            builder.Property(ai => ai.UpdatedAt);

            // ── Relationships ──────────────────────────────────────────────────────
            // Many ActionItems → one Project (delete handled at Project level)
            builder.HasOne(ai => ai.Project)
                .WithMany(p => p.ActionItems)
                .HasForeignKey(ai => ai.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many ActionItems → one Category
            builder.HasOne(ai => ai.Category)
                .WithMany(c => c.ActionItems)
                .HasForeignKey(ai => ai.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Many ActionItems → one SubCategory (nullable)
            builder.HasOne(ai => ai.SubCategory)
                .WithMany(sc => sc.ActionItems)
                .HasForeignKey(ai => ai.SubCategoryId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Many ActionItems → one Owner User (nullable, OwnerId FK)
            builder.HasOne(ai => ai.Owner)
                .WithMany(u => u.OwnedActionItems)
                .HasForeignKey(ai => ai.OwnerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // One ActionItem → one PlannedSchedule (1:1)
            builder.HasOne(ai => ai.PlannedSchedule)
                .WithOne(ps => ps.ActionItem)
                .HasForeignKey<PlannedSchedule>(ps => ps.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // One ActionItem → one ActualExecution (1:1)
            builder.HasOne(ai => ai.ActualExecution)
                .WithOne(ae => ae.ActionItem)
                .HasForeignKey<ActualExecution>(ae => ae.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
