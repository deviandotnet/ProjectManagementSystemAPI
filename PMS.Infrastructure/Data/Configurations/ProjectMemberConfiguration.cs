 using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Entities;
using PMS.Domain.Enums;

namespace PMS.Infrastructure.Data.Configurations
{
    /// <summary>
    /// EF Core fluent configuration for the ProjectMember entity.
    /// Table: ProjectMembers
    /// One user can belong to many projects, each with a different UserRole.
    /// </summary>
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.ToTable("tbl.ps_ProjectMembers");

            builder.HasKey(pm => pm.Id);

            builder.Property(pm => pm.Id)
                .HasDefaultValueSql("NEWID()");

            // Enum stored as tinyint
            builder.Property(pm => pm.Role)
                .IsRequired()
                .HasConversion<byte>();

            builder.Property(pm => pm.JoinedAt)
                .IsRequired()
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Composite unique index: one user can only appear once per project
            builder.HasIndex(pm => new { pm.ProjectId, pm.UserId })
                .IsUnique();

            // ── Audit columns ──────────────────────────────────────────────────────
            builder.Property(pm => pm.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(pm => pm.UpdatedAt);

            // ── Relationships ──────────────────────────────────────────────────────
            // Many ProjectMembers → one Project (configured in ProjectConfiguration)
            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.ProjectMembers)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many ProjectMembers → one User
            builder.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMembers)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Restrict: cannot delete a user who is a member
        }
    }
}
