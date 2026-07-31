using PMS.Domain.ActionItems;
using PMS.Domain.Categories;

using PMS.Domain.ProjectMembers;
using PMS.SharedKernel;

namespace PMS.Domain.Projects
{
    /// <summary>
    /// Top-level entity. Each project is independent with its own calendar and timeline settings.
    /// One project has many: Categories, ProjectMembers, ActionItems.
    /// </summary>
    public class Project : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        /// <summary>0=Sunday to 6=Saturday. Default=1 (Monday). Affects all timeline column generation.</summary>
        public int WeekStartDay { get; set; } = 1;

        public TimelineScale DefaultTimelineScale { get; set; } = TimelineScale.Weekly;
        public ProgressMode ProgressMode { get; set; } = ProgressMode.CountBased;
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;

    }
}
