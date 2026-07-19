namespace PMS.Domain.Entities
{
    /// <summary>
    /// Optional second-level grouping under a Category.
    /// One SubCategory belongs to one Category and can contain many ActionItems.
    /// Note: Color is defined at Category level only, not SubCategory.
    /// </summary>
    public class SubCategory : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        // Navigation Properties
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<ActionItems> ActionItems { get; set; } = new List<ActionItems>();
    }
}
