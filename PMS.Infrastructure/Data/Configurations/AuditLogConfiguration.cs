using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the AuditLog entity.
    /// Table: AuditLogs
    /// High-volume append-only table. Uses BIGINT IDENTITY PK for insert performance.
    /// No update or delete operations should occur on this table.
    /// </summary>
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("tbl.ps_AuditLogs");

            // BIGINT IDENTITY — high-volume insert optimised, no GUID overhead
            builder.HasKey(al => al.Id);

            builder.Property(al => al.Id)
                .UseIdentityAlwaysColumn(); // BIGINT IDENTITY

            builder.Property(al => al.EntityName)
                .IsRequired()
                .HasMaxLength(100);

            // Stores GUID as string for cross-entity flexibility
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

            // Denormalized name — avoids joins on every audit feed query
            builder.Property(al => al.ChangedByName)
                .HasMaxLength(200);

            builder.Property(al => al.IpAddress)
                .HasMaxLength(50);

            builder.Property(al => al.ChangedAt)
                .IsRequired()
                .HasDefaultValueSql("now() at time zone 'utc'");

            // ── Indexes for common query patterns ─────────────────────────────────
            // Most common: "show all changes for this specific record"
            builder.HasIndex(al => new { al.EntityName, al.EntityId });

            // Common: "show all changes by this user"
            builder.HasIndex(al => al.ChangedByUserId);

            // ── Relationship ───────────────────────────────────────────────────────
            // Optional FK to Users — null if performed by a system/seeder process
            builder.HasOne(al => al.ChangedBy)
                .WithMany()
                .HasForeignKey(al => al.ChangedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
