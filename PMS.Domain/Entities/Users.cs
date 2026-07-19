namespace PMS.Domain.Entities
{
    /// <summary>
    /// Registered user account. Authentication uses JWT.
    /// One user can be a member of multiple projects with different roles per project.
    /// </summary>
    public class Users : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

        /// <summary>ActionItems where this user is the assigned owner (OwnerId FK).</summary>
        public virtual ICollection<ActionItems> OwnedActionItems { get; set; } = new List<ActionItems>();

        /// <summary>ActualExecutions where this user is marked as the completer (CompletedById FK).</summary>
        public virtual ICollection<ActualExecution> CompletedExecutions { get; set; } = new List<ActualExecution>();
    }
}
