using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Enums
{
    public enum TaskStatus
    {
        Plan = 0,
        Ongoing = 1,
        Delayed = 2,
        CompletedEarly = 3,
        CompletedOntime = 4,
        CompletedLate = 5,
    }
}
