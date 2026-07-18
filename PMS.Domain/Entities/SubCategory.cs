using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class SubCategory : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string? Color { get; set; }
    }
}
