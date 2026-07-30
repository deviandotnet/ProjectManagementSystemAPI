using PMS.Domain.Users;

namespace PMS.Domain.AuditLogs
{
    /// <summary>
    /// Captures every change made to any entity in the system (structured audit log).
    /// Written automatically by the AuditInterceptor / Application layer on every Create, Update, Delete.
    /// 
    /// NOTE: AuditLog does NOT inherit AuditableBaseEntity — it is the audit record itself.
    ///       It uses a BIGINT identity PK for high-volume write performance.
    /// </summary>
    public class AuditLog
    {
        /// <summary>Auto-incremented identity PK. High-volume optimised — not a GUID.</summary>
        public long Id { get; set; }

        /// <summary>Name of the entity that changed (e.g., "ActionItem", "PlannedSchedule").</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>The GUID of the changed record, stored as string for flexibility.</summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>Operation performed: "Create", "Update", or "Delete".</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>The specific field that changed (e.g., "PlannedStartDate"). Null for Create/Delete.</summary>
        public string? FieldName { get; set; }

        public string? OldValue { get; set; }
        public string? NewValue { get; set; }

        /// <summary>FK to Users.Id — nullable if action was performed by a system/seeder process.</summary>
        public Guid? ChangedByUserId { get; set; }

        /// <summary>Denormalized name stored for display — avoids join on every audit feed query.</summary>
        public string? ChangedByName { get; set; }

        public DateTimeOffset ChangedAt { get; set; }
        public string? IpAddress { get; set; }

        // Navigation Properties
        public virtual User? ChangedBy { get; set; }
    }
}
