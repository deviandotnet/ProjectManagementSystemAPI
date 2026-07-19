namespace PMS.Domain.Entities
{
    /// <summary>
    /// Tracks actual execution data for one ActionItem (ACTUAL columns from original spreadsheet).
    /// 1:1 with ActionItems. Created alongside the ActionItem; all date fields start as null.
    /// Dates use DateOnly (maps to SQL DATE) — no time component needed.
    /// </summary>
    public class ActualExecution
    {
        public Guid Id { get; set; }
        public Guid ActionItemId { get; set; }
        public DateOnly? ActualStartDate { get; set; }
        public DateOnly? ActualEndDate { get; set; }
        public decimal? ActualHours { get; set; }
        public string? CompletedByName { get; set; }

        /// <summary>Optional FK to a registered user who completed the item.</summary>
        public Guid? CompletedById { get; set; }

        /// <summary>Reason for delay — filled in when item completes after PlannedEndDate.</summary>
        public string? DelayReason { get; set; }

        public string? ActualRemarks { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation Properties
        public virtual ActionItems ActionItem { get; set; } = null!;

        /// <summary>Navigation to the registered user who completed this item (via CompletedById FK).</summary>
        public virtual Users? CompletedBy { get; set; }
    }
}
