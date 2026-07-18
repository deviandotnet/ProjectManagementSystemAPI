using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public class Project : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public int WeekStartDay { get; set; } // (0=Sun to 6=Sat, Default=1 for Monday)
        public int DefaultTimelineScale { get; set; } // (TimelineScale enum, Default=1 Weekly)
    }
}
