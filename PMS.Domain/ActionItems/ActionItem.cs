using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.SharedKernel;


namespace PMS.Domain.ActionItems
{
    /// <summary>
    /// Core entity. Every row in the timeline grid is one ActionItem.
    /// Belongs to a Project, a Category, and optionally a SubCategory.
    /// Has a 1:1 relationship with PlannedSchedule and ActualExecution.
    /// NOTE: ComputedStatus is NEVER stored — it is derived at runtime by the StatusEngine.
    /// </summary>
    public class ActionItem : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string ActionItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; } = Priority.Medium;
        public string? OwnerName { get; set; }

        /// <summary>Optional FK link to a registered user. OwnerName is still stored as free text.</summary>
        public Guid? OwnerId { get; set; }

        /// <summary>Used when Project.ProgressMode = WeightBased. Value range: 0–100.</summary>
        public decimal? Weight { get; set; }

        /// <summary>Display order within category/subcategory grouping.</summary>
        public int Sequence { get; set; }
        public string? Remarks { get; set; }

    }
}
