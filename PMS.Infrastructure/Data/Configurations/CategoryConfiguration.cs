using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the Category entity.
    /// Table: Categories
    /// One Category belongs to one Project and groups many SubCategories and ActionItems.
    /// </summary>
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("tbl.ps_Categories");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Id)
                .HasDefaultValueSql("NEWID()");

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.DisplayOrder)
                .IsRequired()
                .HasDefaultValue(0);

            // Hex color code e.g. #3A86FF — used for timeline row color coding
            builder.Property(c => c.Color)
                .HasMaxLength(7);

            // ── Audit columns ──────────────────────────────────────────────────────
            builder.Property(c => c.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(c => c.UpdatedAt);

            // ── Relationships ──────────────────────────────────────────────────────
            // Many Categories → one Project (configured in ProjectConfiguration)
            builder.HasOne(c => c.Project)
                .WithMany(p => p.Categories)
                .HasForeignKey(c => c.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Category → many SubCategories
            builder.HasMany(c => c.SubCategories)
                .WithOne(sc => sc.Category)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Category → many ActionItems
            builder.HasMany(c => c.ActionItems)
                .WithOne(ai => ai.Category)
                .HasForeignKey(ai => ai.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict: ActionItems must be reassigned before deleting category
        }
    }
}
