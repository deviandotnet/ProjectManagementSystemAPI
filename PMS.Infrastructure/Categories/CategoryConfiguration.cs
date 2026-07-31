using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Categories;
using PMS.Domain.Projects;

namespace PMS.Infrastructure.Categories;

/// <summary>
/// EF Core fluent configuration for the Category entity.
/// Table: tbl.ps_Categories
/// One Category belongs to one Project and groups SubCategories and ActionItems.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("tbl.ps_Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.Color)
            .HasMaxLength(7);

        // ── Audit columns ──────────────────────────────────────────────────────
        builder.Property(c => c.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(c => c.UpdatedAt);

        // ── Relationships ──────────────────────────────────────────────────────
        // Many Categories → one Project
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
