using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class PlannedSchedule
    {
        public Guid Id { get; set; }
        public Guid ActionItemId { get; set; }
        public  DateTimeOffset PlannedStartDate { get; set; }
        public DateTimeOffset PlannedEndDate { get; set; }
        public string PlannedStartWeek { get; set; } = string.Empty; //COMPUTED (e.g., WW03) 
        public string PlannedEndWeek { get; set; } = string.Empty; //COMPUTED(e.g., WW04) 
        public int DurationCalendarDays { get; set; } //(PlannedEndDate - PlannedStartDate) 
        public int DurationWorkingDays { get; set; } //(PlannedEndDate - PlannedStartDate) excluding weekends and holidays
    }
}
