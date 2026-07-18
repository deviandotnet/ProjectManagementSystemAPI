using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class ActualExecution
    {
        public Guid Id { get; set; }
        public Guid ActionItemId { get; set; }
        public DateTimeOffset? ActualStartDate { get; set; }
        public DateTimeOffset? ActualEndDate { get; set; }
        public decimal? ActualHours { get; set; }
        public string? CompletedByName { get; set; }
        public Guid? CompletedById { get; set; }
        public string? Remarks { get; set; }

    }
}
