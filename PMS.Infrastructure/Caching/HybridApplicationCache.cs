using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using PMS.Application.Abstractions.Caching;

namespace PMS.Infrastructure.Caching;

internal sealed class HybridApplicationCache(
    HybridCache cache,
    CacheOptions configuration,
    ILogger<HybridApplicationCache> logger) : IApplicationCache
{
    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        ApplicationCacheEntryOptions options,
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.Enabled)
        {
            return await factory(cancellationToken);
        }

        bool factoryStarted = false;
        bool factoryCompleted = false;
        T? generatedValue = default;

        try
        {
            return await cache.GetOrCreateAsync(
                key,
                async token =>
                {
                    factoryStarted = true;
                    generatedValue = await factory(token);
                    factoryCompleted = true;
                    return generatedValue;
                },
                new HybridCacheEntryOptions
                {
                    Expiration = options.Expiration,
                    LocalCacheExpiration = options.LocalCacheExpiration
                },
                tags,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (factoryCompleted)
        {
            logger.LogWarning(exception, "Cache write failed for key {CacheKey}; returning generated value.", key);
            return generatedValue!;
        }
        catch (Exception exception) when (!factoryStarted)
        {
            logger.LogWarning(exception, "Cache read failed for key {CacheKey}; using the data source.", key);
            return await factory(cancellationToken);
        }
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!configuration.Enabled)
        {
            return;
        }

        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache key invalidation failed for {CacheKey}.", key);
        }
    }

    public async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (!configuration.Enabled)
        {
            return;
        }

        try
        {
            await cache.RemoveByTagAsync(tag, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache tag invalidation failed for {CacheTag}.", tag);
        }
    }

    public async ValueTask RemoveByTagsAsync(
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.Enabled || tags.Count == 0)
        {
            return;
        }

        try
        {
            await cache.RemoveByTagAsync(tags, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Cache tag invalidation failed for {CacheTags}.", tags);
        }
    }
}
