using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the ActualExecution entity.
    /// Table: ActualExecutions
    /// 1:1 with ActionItems. Created alongside the ActionItem; all date fields start as null.
    /// All date columns map to SQL DATE (DateOnly in C#) — no time component stored.
    /// </summary>
    public class ActualExecutionConfiguration : IEntityTypeConfiguration<ActualExecution>
    {
        public void Configure(EntityTypeBuilder<ActualExecution> builder)
        {
            builder.ToTable("tbl.ps_ActualExecutions");

            builder.HasKey(ae => ae.Id);

            builder.Property(ae => ae.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            // Enforce 1:1 — only one ActualExecution per ActionItem allowed
            builder.HasIndex(ae => ae.ActionItemId)
                .IsUnique();

            // DateOnly → SQL DATE — nullable: null means not yet started / not yet completed
            builder.Property(ae => ae.ActualStartDate)
                .HasColumnType("date");

            builder.Property(ae => ae.ActualEndDate)
                .HasColumnType("date");

            builder.Property(ae => ae.ActualHours)
                .HasColumnType("decimal(8,2)");

            builder.Property(ae => ae.CompletedByName)
                .HasMaxLength(200);

            builder.Property(ae => ae.DelayReason)
                .HasColumnType("text");

            builder.Property(ae => ae.ActualRemarks)
                .HasColumnType("text");

            builder.Property(ae => ae.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");

            builder.Property(ae => ae.UpdatedAt);

            // ── Relationships ──────────────────────────────────────────────────────
            // 1:1 back-reference — principal side configured in ActionItemConfiguration
            builder.HasOne(ae => ae.ActionItem)
                .WithOne(ai => ai.ActualExecution)
                .HasForeignKey<ActualExecution>(ae => ae.ActionItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many ActualExecutions → one CompletedBy User (nullable)
            builder.HasOne(ae => ae.CompletedBy)
                .WithMany(u => u.CompletedExecutions)
                .HasForeignKey(ae => ae.CompletedById)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
