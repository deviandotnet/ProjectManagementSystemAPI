using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public abstract class AuditableBaseEntity
    {
        public Guid CreatedBy { get; set; }
        public Guid? ModifiedBy { get; set; }
        public DateTimeOffset CreatedOn { get; set; }
        public DateTimeOffset? ModifiedOn { get; set; }
    }
}
