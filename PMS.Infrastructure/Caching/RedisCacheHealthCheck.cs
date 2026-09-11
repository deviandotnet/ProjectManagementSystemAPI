using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace PMS.Infrastructure.Caching;

internal sealed class RedisCacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string key = $"pms:health:{Guid.NewGuid():N}";
        byte[] expected = Encoding.UTF8.GetBytes("ok");
        bool entryWritten = false;

        try
        {
            await cache.SetAsync(
                key,
                expected,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                },
                cancellationToken);
            entryWritten = true;

            byte[]? actual = await cache.GetAsync(key, cancellationToken);

            return actual is not null && actual.AsSpan().SequenceEqual(expected)
                ? HealthCheckResult.Healthy("Redis cache is writable and readable.")
                : new HealthCheckResult(
                    context.Registration.FailureStatus,
                    "Redis cache read-after-write verification failed.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Redis cache connectivity check failed.",
                exception);
        }
        finally
        {
            try
            {
                if (entryWritten)
                {
                    await cache.RemoveAsync(key, cancellationToken);
                }
            }
            catch
            {
                // The entry expires quickly; cleanup must not replace the probe result.
            }
        }
    }
}
