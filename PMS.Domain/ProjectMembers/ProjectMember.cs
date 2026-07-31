
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Domain.ProjectMembers
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
    }
}
