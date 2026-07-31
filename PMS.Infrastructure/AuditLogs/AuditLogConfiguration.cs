using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.AuditLogs;
using PMS.Domain.Users;

namespace PMS.Infrastructure.AuditLogs;

/// <summary>
/// EF Core fluent configuration for the AuditLog entity.
/// Table: tbl.ps_AuditLogs
/// High-volume append-only table. Uses BIGINT IDENTITY PK for insert performance.
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("tbl.ps_AuditLogs");

        builder.HasKey(al => al.Id);

        builder.Property(al => al.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(al => al.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(al => al.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(al => al.FieldName)
            .HasMaxLength(100);

        builder.Property(al => al.OldValue)
            .HasColumnType("text");

        builder.Property(al => al.NewValue)
            .HasColumnType("text");

        builder.Property(al => al.ChangedByName)
            .HasMaxLength(200);

        builder.Property(al => al.IpAddress)
            .HasMaxLength(50);

        builder.Property(al => al.ChangedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // ── Indexes ────────────────────────────────────────────────────────────
        builder.HasIndex(al => new { al.EntityName, al.EntityId });
        builder.HasIndex(al => al.ChangedByUserId);

        // ── Relationships (Shadow-Style) ───────────────────────────────────────
        // Optional FK to User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(al => al.ChangedByUserId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
