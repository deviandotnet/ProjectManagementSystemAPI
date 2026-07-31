namespace PMS.SharedKernel
{
    /// <summary>
    /// Base class for all auditable entities. Tracks who created/updated a record and when.
    /// Inherited by: Users, Project, ProjectMember, Category, SubCategory, ActionItems.
    /// NOTE: PlannedSchedule, ActualExecution, HolidayCalendar, and AuditLog own their own timestamps.
    /// </summary>
    public abstract class AuditableBaseEntity
    {
        private readonly List<IDomainEvent> _domainEvents = [];

        public Guid? CreatedByUserId { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public List<IDomainEvent> DomainEvents => [.. _domainEvents];

        public void Raise(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
