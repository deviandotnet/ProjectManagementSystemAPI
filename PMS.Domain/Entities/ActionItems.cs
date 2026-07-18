using PMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class ActionItems : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid CategoryId { get; set; }
        public Guid? SubCategoryId { get; set; }
        public string ActionItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Priority Priority { get; set; } // (0=Low, 1=Medium, 2=High, 3=Critical)
        public string? OwnerName { get; set; }
        public Guid? OwnerId { get; set; }
        public decimal? Weight { get; set; } //(used when ProgressMode = WeightBased, 0-100%)
        public int Sequence { get; set; }
        public string? Remarks { get; set; }



    }
}
