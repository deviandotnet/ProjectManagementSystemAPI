using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PMS.Domain.Users;

namespace PMS.Infrastructure.Users
{
    /// <summary>
    /// EF Core fluent configuration for the Users entity.
    /// Table: Users
    /// </summary>
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("tbl.ps_Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.MiddleName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            // ── Audit columns ──────────────────────────────────────────────────────
            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("now() at time zone 'utc'");

            builder.Property(u => u.UpdatedAt);

            // ── Inverse navigation relationships ───────────────────────────────────
            // ProjectMember → configured in ProjectMemberConfiguration
            // OwnedActionItems → configured in ActionItemConfiguration
            // CompletedExecutions → configured in ActualExecutionConfiguration
        }
    }
}
