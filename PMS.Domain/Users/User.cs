using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.ProjectMembers;
using PMS.SharedKernel;

namespace PMS.Domain.Users
{
    /// <summary>
    /// Registered user account. Authentication uses JWT.
    /// One user can be a member of multiple projects with different roles per project.
    /// </summary>
    public class User : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

    }
}
