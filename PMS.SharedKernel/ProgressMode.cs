namespace PMS.SharedKernel
{
    public enum ProgressMode
    {
        CountBased = 1, //(Completed Tasks / Total Tasks)
        WeightBased = 2,  //(Sum of Weight × Completion %)
    }
}
