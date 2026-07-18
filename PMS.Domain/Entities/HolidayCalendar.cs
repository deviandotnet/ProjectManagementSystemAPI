using PMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class HolidayCalendar
    {
        public Guid Id { get; set; }
        public DateTimeOffset HolidayDate { get; set; }
        public string Name { get; set; } = string.Empty;
        public HolidayType HolidayTypes { get; set; }
        public bool IsRecurringAnnually { get; set; }
        public int? Year { get; set; } //(NULL if recurring)
    }
}
