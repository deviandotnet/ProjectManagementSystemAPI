namespace PMS.SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
