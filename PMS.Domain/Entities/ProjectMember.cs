using PMS.Domain.Enums;

namespace PMS.Domain.Entities
{
    /// <summary>
    /// Assigns a registered user to a project with a specific per-project role.
    /// One user can belong to many projects, each with a different UserRole.
    /// </summary>
    public class ProjectMember : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public UserRole Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }

        // Navigation Properties
        public virtual Project Project { get; set; } = null!;
        public virtual Users User { get; set; } = null!;
    }
}
