using PMS.Domain.Enums;

namespace PMS.Domain.Entities
{
    /// <summary>
    /// Global holiday calendar — shared across all projects.
    /// Used by CalendarEngine to exclude non-working days from DurationWorkingDays calculation.
    /// HolidayDate uses DateOnly (maps to SQL DATE) — no time component needed.
    /// Pre-seeded with Philippine national holidays on startup via HolidaySeeder.
    /// </summary>
    public class HolidayCalendar
    {
        public Guid Id { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public HolidayType Type { get; set; }

        /// <summary>If true, Year is NULL and the holiday repeats every year on the same date.</summary>
        public bool IsRecurringAnnually { get; set; }

        /// <summary>NULL when IsRecurringAnnually is true. Populated for one-off holidays.</summary>
        public int? Year { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
