using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;

namespace PMS.Infrastructure.ProjectMembers;

/// <summary>
/// EF Core fluent configuration for the ProjectMember entity.
/// Table: tbl.ps_ProjectMembers
/// Configured using shadow-style relationship definitions per clean architecture conventions.
/// </summary>
internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("tbl.ps_ProjectMembers");

        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.Id)
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(pm => pm.Role)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(pm => pm.JoinedAt)
            .IsRequired()
            .HasDefaultValueSql("now() at time zone 'utc'");

        // Composite unique index: one user can only appear once per project
        builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
            .IsUnique();

        // ── Audit columns ──────────────────────────────────────────────────────
        builder.Property(pm => pm.CreatedAt)
            .HasDefaultValueSql("now() at time zone 'utc'");

        builder.Property(pm => pm.UpdatedAt);

        // ── Relationships ──────────────────────────────────────────────────────
        // Many ProjectMembers → one Project
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(pm => pm.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many ProjectMembers → one User
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pm => pm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
