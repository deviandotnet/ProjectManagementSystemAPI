using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Categories;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;

namespace PMS.Infrastructure.SubCategories;

/// <summary>
/// EF Core fluent configuration for the SubCategory entity.
/// Table: tbl.ps_SubCategories
/// One SubCategory belongs to one Category and optionally groups ActionItems.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class SubCategoryConfiguration : IEntityTypeConfiguration<SubCategory>
{
    public void Configure(EntityTypeBuilder<SubCategory> builder)
    {
        builder.ToTable("tbl.ps_SubCategories");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(sc => sc.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(sc => sc.DisplayOrder)
            .IsRequired()
            .HasDefaultValue(0);

        // ── Audit columns ──────────────────────────────────────────────────────
        builder.Property(sc => sc.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(sc => sc.UpdatedAt);

        // ── Relationships ──────────────────────────────────────────────────────
        // Many SubCategories → one Category
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(sc => sc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
