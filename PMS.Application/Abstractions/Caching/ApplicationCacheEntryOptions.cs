namespace PMS.Application.Abstractions.Caching;

public sealed record ApplicationCacheEntryOptions(
    TimeSpan Expiration,
    TimeSpan LocalCacheExpiration);
