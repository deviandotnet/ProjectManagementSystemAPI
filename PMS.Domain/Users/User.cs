using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.ProjectMembers;
using PMS.SharedKernel;

namespace PMS.Domain.Users
{
    /// <summary>
    /// Registered user account. Authentication uses JWT.
    /// Has a global SystemRole (User or Admin) and per-project roles via ProjectMember.
    /// </summary>
    public class User : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public SystemRole SystemRole { get; set; } = SystemRole.User;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
        public virtual ICollection<ActionItem> OwnedActionItems { get; set; } = new List<ActionItem>();
        public virtual ICollection<ActualExecution> CompletedExecutions { get; set; } = new List<ActualExecution>();
    }
}
