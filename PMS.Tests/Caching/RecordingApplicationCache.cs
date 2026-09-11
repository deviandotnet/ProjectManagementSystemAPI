using PMS.Application.Abstractions.Caching;

namespace PMS.UnitTests.Caching;

internal sealed class RecordingApplicationCache : IApplicationCache
{
    private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _keysByTag = new(StringComparer.Ordinal);

    public int FactoryCalls { get; private set; }

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        ApplicationCacheEntryOptions options,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        if (_entries.TryGetValue(key, out object? cached))
        {
            return (T)cached!;
        }

        FactoryCalls++;
        T value = await factory(cancellationToken);
        _entries[key] = value;

        foreach (string tag in tags)
        {
            if (!_keysByTag.TryGetValue(tag, out HashSet<string>? keys))
            {
                keys = new HashSet<string>(StringComparer.Ordinal);
                _keysByTag[tag] = keys;
            }

            keys.Add(key);
        }

        return value;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.Remove(key);
        foreach (HashSet<string> keys in _keysByTag.Values)
        {
            keys.Remove(key);
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (_keysByTag.Remove(tag, out HashSet<string>? keys))
        {
            foreach (string key in keys)
            {
                _entries.Remove(key);
            }
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask RemoveByTagsAsync(
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        foreach (string tag in tags)
        {
            await RemoveByTagAsync(tag, cancellationToken);
        }
    }
}
