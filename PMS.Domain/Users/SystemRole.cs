namespace PMS.Domain.Users;

/// <summary>
/// Global system-level role. Embedded in JWT claims.
/// </summary>
public enum SystemRole : byte
{
    /// <summary>Standard system user. Can participate in project features based on project-level role.</summary>
    User = 1,

    /// <summary>System administrator. Has override capability to access and manage all system features.</summary>
    Admin = 2
}
