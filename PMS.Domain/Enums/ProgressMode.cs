using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Domain.Enums
{
    public enum ProgressMode
    {
        CountBased = 1, //(Completed Tasks / Total Tasks)
        WeightBased = 2,  //(Sum of Weight × Completion %)
    }
}
