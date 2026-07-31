using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;

namespace PMS.Infrastructure.ActionItems;

/// <summary>
/// EF Core fluent configuration for the ActionItem entity.
/// Table: tbl.ps_ActionItems
/// Core entity — every row in the timeline grid is one ActionItem.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// ComputedStatus is NEVER stored; it is derived at runtime by StatusEngine.
/// </summary>
internal sealed class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
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
            .IsRequired(false);

        builder.Property(ai => ai.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(ai => ai.UpdatedAt);

        // ── Relationships ──────────────────────────────────────────────────────
        // Many ActionItems → one Project
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(ai => ai.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many ActionItems → one Category
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(ai => ai.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many ActionItems → one SubCategory (optional)
        builder.HasOne<SubCategory>()
            .WithMany()
            .HasForeignKey(ai => ai.SubCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Many ActionItems → one Owner User (optional)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ai => ai.OwnerId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
