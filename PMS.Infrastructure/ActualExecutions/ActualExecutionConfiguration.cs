using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Users;

namespace PMS.Infrastructure.ActualExecutions;

/// <summary>
/// EF Core fluent configuration for the ActualExecution entity.
/// Table: tbl.ps_ActualExecutions
/// 1:1 with ActionItem. Created alongside the ActionItem; all date fields start as null.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class ActualExecutionConfiguration : IEntityTypeConfiguration<ActualExecution>
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
        // 1:1 with ActionItem — ActualExecution is the dependent side holding ActionItemId
        builder.HasOne<ActionItem>()
            .WithOne()
            .HasForeignKey<ActualExecution>(ae => ae.ActionItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many ActualExecutions → one CompletedBy User (optional)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ae => ae.CompletedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
