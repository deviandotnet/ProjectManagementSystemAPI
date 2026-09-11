namespace PMS.Application.Abstractions.Caching;

public static class CachePolicy
{
    public static readonly ApplicationCacheEntryOptions Entity = new(
        TimeSpan.FromMinutes(15),
        TimeSpan.FromMinutes(1));

    public static readonly ApplicationCacheEntryOptions Collection = new(
        TimeSpan.FromMinutes(10),
        TimeSpan.FromMinutes(1));

    public static readonly ApplicationCacheEntryOptions Computed = new(
        TimeSpan.FromMinutes(5),
        TimeSpan.FromSeconds(30));

    public static readonly ApplicationCacheEntryOptions Reference = new(
        TimeSpan.FromHours(12),
        TimeSpan.FromMinutes(10));

    public static readonly ApplicationCacheEntryOptions Audit = new(
        TimeSpan.FromMinutes(5),
        TimeSpan.FromSeconds(30));
}
