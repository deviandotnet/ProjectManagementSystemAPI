namespace PMS.Application.Abstractions.Caching;

public interface IApplicationCache
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        ApplicationCacheEntryOptions options,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);

    ValueTask RemoveByTagAsync(
        string tag,
        CancellationToken cancellationToken = default);

    ValueTask RemoveByTagsAsync(
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default);
}
