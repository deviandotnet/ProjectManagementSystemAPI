using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text;

namespace PMS.Domain.Entities
{
    public sealed class ProjectMember : AuditableBaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public int Role { get; set; }
        public DateTimeOffset JoinedAt { get; set; }

        //**Note:** One user can be a member of multiple projects with different roles per project.
    }
}
