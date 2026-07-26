using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the SubCategory entity.
    /// Table: SubCategories
    /// One SubCategory belongs to one Category and optionally groups ActionItems.
    /// </summary>
    public class SubCategoryConfiguration : IEntityTypeConfiguration<SubCategory>
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
            // Many SubCategories → one Category (configured in CategoryConfiguration)
            builder.HasOne(sc => sc.Category)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(sc => sc.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            // One SubCategory → many ActionItems
            builder.HasMany(sc => sc.ActionItems)
                .WithOne(ai => ai.SubCategory)
                .HasForeignKey(ai => ai.SubCategoryId)
                .OnDelete(DeleteBehavior.SetNull); // SetNull: removing a sub-category detaches its items gracefully
        }
    }
}
