
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.SharedKernel;

namespace PMS.Domain.Categories
{
    /// <summary>
    /// High-level grouping of ActionItems within a project.
    /// One Category belongs to one Project and can contain many SubCategories and ActionItems.
    /// </summary>
    public class Category : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        /// <summary>Hex color string (e.g. #3A86FF). Used for timeline row color coding.</summary>
        public string? Color { get; set; }

    }
}
