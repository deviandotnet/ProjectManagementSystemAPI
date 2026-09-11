namespace PMS.Application.Abstractions.Caching;

public sealed class NullApplicationCache : IApplicationCache
{
    public static readonly NullApplicationCache Instance = new();

    private NullApplicationCache()
    {
    }

    public ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        ApplicationCacheEntryOptions options,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default) => factory(cancellationToken);

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask RemoveByTagsAsync(
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
