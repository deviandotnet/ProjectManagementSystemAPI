using PMS.Domain.ActionItems;

namespace PMS.Domain.PlannedSchedules
{
    /// <summary>
    /// Planning data for one ActionItem (PLAN columns from original spreadsheet).
    /// 1:1 with ActionItems — one planned schedule per action item.
    /// PlannedStartWeek and PlannedEndWeek are COMPUTED values (e.g., "WW03"), not stored by user input.
    /// DurationCalendarDays is COMPUTED. DurationWorkingDays is calculated via CalendarEngine.
    /// Dates use DateOnly (maps to SQL DATE) — no time component needed.
    /// </summary>
    public class PlannedSchedule
    {
        public Guid Id { get; set; }
        public Guid ActionItemId { get; set; }
        public DateOnly PlannedStartDate { get; set; }
        public DateOnly PlannedEndDate { get; set; }

        /// <summary>COMPUTED — e.g., "WW03". Derived from PlannedStartDate on save.</summary>
        public string PlannedStartWeek { get; set; } = string.Empty;

        /// <summary>COMPUTED — e.g., "WW07". Derived from PlannedEndDate on save.</summary>
        public string PlannedEndWeek { get; set; } = string.Empty;

        /// <summary>COMPUTED — total calendar days between PlannedStartDate and PlannedEndDate.</summary>
        public int DurationCalendarDays { get; set; }

        /// <summary>Calculated by CalendarEngine — excludes weekends and HolidayCalendar entries.</summary>
        public int DurationWorkingDays { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
