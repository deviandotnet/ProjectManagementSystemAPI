namespace PMS.Domain.Entities
{
    /// <summary>
    /// Base class for all auditable entities. Tracks who created/updated a record and when.
    /// Inherited by: Users, Project, ProjectMember, Category, SubCategory, ActionItems.
    /// NOTE: PlannedSchedule, ActualExecution, HolidayCalendar, and AuditLog own their own timestamps.
    /// </summary>
    public abstract class AuditableBaseEntity
    {
        public Guid CreatedByUserId { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
