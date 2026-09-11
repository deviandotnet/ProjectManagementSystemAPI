using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PMS.Application.Abstractions.Caching;
using PMS.Infrastructure.Caching;
using System.Collections.Concurrent;

namespace PMS.UnitTests.Caching;

public sealed class HybridApplicationCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_Should_InvokeFactoryOnce_WhenEntryIsCached()
    {
        await using ServiceProvider provider = CreateProvider(enabled: true);
        IApplicationCache cache = provider.GetRequiredService<IApplicationCache>();
        int factoryCalls = 0;

        ValueTask<string> Factory(CancellationToken _)
        {
            factoryCalls++;
            return ValueTask.FromResult("cached-value");
        }

        string first = await cache.GetOrCreateAsync(
            "pms:v1:test:item",
            Factory,
            CachePolicy.Entity,
            ["pms:test:tag"],
            CancellationToken.None);

        string second = await cache.GetOrCreateAsync(
            "pms:v1:test:item",
            Factory,
            CachePolicy.Entity,
            ["pms:test:tag"],
            CancellationToken.None);

        first.Should().Be("cached-value");
        second.Should().Be("cached-value");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task RemoveByTagAsync_Should_InvalidateEveryKeyWithThatTag()
    {
        await using ServiceProvider provider = CreateProvider(enabled: true);
        IApplicationCache cache = provider.GetRequiredService<IApplicationCache>();
        int firstFactoryCalls = 0;
        int secondFactoryCalls = 0;

        await cache.GetOrCreateAsync(
            "pms:v1:test:first",
            _ => ValueTask.FromResult(++firstFactoryCalls),
            CachePolicy.Entity,
            ["pms:test:shared"],
            CancellationToken.None);

        await cache.GetOrCreateAsync(
            "pms:v1:test:second",
            _ => ValueTask.FromResult(++secondFactoryCalls),
            CachePolicy.Entity,
            ["pms:test:shared"],
            CancellationToken.None);

        await cache.RemoveByTagAsync("pms:test:shared", CancellationToken.None);

        await cache.GetOrCreateAsync(
            "pms:v1:test:first",
            _ => ValueTask.FromResult(++firstFactoryCalls),
            CachePolicy.Entity,
            ["pms:test:shared"],
            CancellationToken.None);

        await cache.GetOrCreateAsync(
            "pms:v1:test:second",
            _ => ValueTask.FromResult(++secondFactoryCalls),
            CachePolicy.Entity,
            ["pms:test:shared"],
            CancellationToken.None);

        firstFactoryCalls.Should().Be(2);
        secondFactoryCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_BypassStorage_WhenCachingIsDisabled()
    {
        await using ServiceProvider provider = CreateProvider(enabled: false);
        IApplicationCache cache = provider.GetRequiredService<IApplicationCache>();
        int factoryCalls = 0;

        for (int index = 0; index < 2; index++)
        {
            await cache.GetOrCreateAsync(
                "pms:v1:test:disabled",
                _ => ValueTask.FromResult(++factoryCalls),
                CachePolicy.Entity,
                ["pms:test:disabled"],
                CancellationToken.None);
        }

        factoryCalls.Should().Be(2);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_NotRetryOrCacheFactoryFailures()
    {
        await using ServiceProvider provider = CreateProvider(enabled: true);
        IApplicationCache cache = provider.GetRequiredService<IApplicationCache>();
        int factoryCalls = 0;

        Func<Task> action = async () => await cache.GetOrCreateAsync<string>(
            "pms:v1:test:factory-failure",
            _ =>
            {
                factoryCalls++;
                throw new InvalidOperationException("data source failed");
            },
            CachePolicy.Entity,
            ["pms:test:failure"],
            CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("data source failed");
        factoryCalls.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_ReturnDataSourceValue_WhenDistributedCacheIsUnavailable()
    {
        await using ServiceProvider provider = CreateProvider(enabled: true, new ThrowingDistributedCache());
        IApplicationCache cache = provider.GetRequiredService<IApplicationCache>();

        string value = await cache.GetOrCreateAsync(
            "pms:v1:test:redis-outage",
            _ => ValueTask.FromResult("from-database"),
            CachePolicy.Entity,
            ["pms:test:outage"],
            CancellationToken.None);

        value.Should().Be("from-database");
    }

    [Fact]
    public void AddApplicationCaching_Should_FailFast_WhenRedisIsEnabledWithoutConnectionString()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:Redis:Enabled"] = "true"
            })
            .Build();
        var services = new ServiceCollection();

        Action action = () => services.AddApplicationCaching(configuration);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Redis connection string*");
    }

    [Fact]
    public void AddApplicationCaching_Should_AcceptConnectionStringsRedisFallback()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:Redis:Enabled"] = "true",
                ["Caching:Redis:ConnectionString"] = "",
                ["ConnectionStrings:Redis"] = "localhost:6379"
            })
            .Build();
        var services = new ServiceCollection();

        Action action = () => services.AddApplicationCaching(configuration);

        action.Should().NotThrow();
    }

    [Fact]
    public void AddApplicationCaching_Should_ConfigureHybridCacheSafetyLimits()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Caching:MaximumPayloadBytes"] = "262144",
                ["Caching:MaximumKeyLength"] = "512",
                ["Caching:ReportTagMetrics"] = "true"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationCaching(configuration);

        using ServiceProvider provider = services.BuildServiceProvider();
        HybridCacheOptions options = provider.GetRequiredService<IOptions<HybridCacheOptions>>().Value;

        options.MaximumPayloadBytes.Should().Be(262144);
        options.MaximumKeyLength.Should().Be(512);
        options.ReportTagMetrics.Should().BeTrue();
    }

    [Theory]
    [InlineData("Caching:MaximumPayloadBytes")]
    [InlineData("Caching:MaximumKeyLength")]
    public void AddApplicationCaching_Should_RejectNonPositiveSafetyLimits(string key)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [key] = "0" })
            .Build();
        var services = new ServiceCollection();

        Action action = () => services.AddApplicationCaching(configuration);

        action.Should().Throw<InvalidOperationException>().WithMessage($"*{key}*");
    }

    [Fact]
    public async Task RedisCacheHealthCheck_Should_VerifyDistributedCacheRoundTrip()
    {
        var healthCheck = new RedisCacheHealthCheck(new SharedTestDistributedCache());
        var registration = new HealthCheckRegistration(
            "Redis Cache",
            healthCheck,
            HealthStatus.Degraded,
            ["ready", "cache"]);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext { Registration = registration },
            CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task RedisCacheHealthCheck_Should_ReportConfiguredFailureStatus_WhenRedisIsUnavailable()
    {
        var healthCheck = new RedisCacheHealthCheck(new ThrowingDistributedCache());
        var registration = new HealthCheckRegistration(
            "Redis Cache",
            healthCheck,
            HealthStatus.Degraded,
            ["ready", "cache"]);

        HealthCheckResult result = await healthCheck.CheckHealthAsync(
            new HealthCheckContext { Registration = registration },
            CancellationToken.None);

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task HybridCache_Should_ShareL2AcrossProviders_AndHonorDistributedTagInvalidation()
    {
        IDistributedCache sharedL2 = new SharedTestDistributedCache();

        await using ServiceProvider firstProvider = CreateProvider(enabled: true, sharedL2);
        await using ServiceProvider secondProvider = CreateProvider(enabled: true, sharedL2);
        IApplicationCache firstCache = firstProvider.GetRequiredService<IApplicationCache>();
        IApplicationCache secondCache = secondProvider.GetRequiredService<IApplicationCache>();
        int secondFactoryCalls = 0;

        await firstCache.GetOrCreateAsync(
            "pms:v1:test:l2",
            _ => ValueTask.FromResult("from-first-provider"),
            CachePolicy.Entity,
            ["pms:test:l2-tag"],
            CancellationToken.None);

        string fromSharedL2 = await secondCache.GetOrCreateAsync(
            "pms:v1:test:l2",
            _ =>
            {
                secondFactoryCalls++;
                return ValueTask.FromResult("from-second-provider");
            },
            CachePolicy.Entity,
            ["pms:test:l2-tag"],
            CancellationToken.None);

        fromSharedL2.Should().Be("from-first-provider");
        secondFactoryCalls.Should().Be(0);

        await firstCache.RemoveByTagAsync("pms:test:l2-tag", CancellationToken.None);

        await using ServiceProvider thirdProvider = CreateProvider(enabled: true, sharedL2);
        IApplicationCache thirdCache = thirdProvider.GetRequiredService<IApplicationCache>();
        int thirdFactoryCalls = 0;
        string refreshed = await thirdCache.GetOrCreateAsync(
            "pms:v1:test:l2",
            _ =>
            {
                thirdFactoryCalls++;
                return ValueTask.FromResult("refreshed");
            },
            CachePolicy.Entity,
            ["pms:test:l2-tag"],
            CancellationToken.None);

        refreshed.Should().Be("refreshed");
        thirdFactoryCalls.Should().Be(1);
    }

    private static ServiceProvider CreateProvider(bool enabled, IDistributedCache? distributedCache = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (distributedCache is not null)
        {
            services.AddSingleton(distributedCache);
        }
        services.AddHybridCache();
        services.AddSingleton(new CacheOptions { Enabled = enabled });
        services.AddSingleton<IApplicationCache, HybridApplicationCache>();
        return services.BuildServiceProvider();
    }

    private sealed class SharedTestDistributedCache : IDistributedCache
    {
        private readonly ConcurrentDictionary<string, byte[]> _entries = new(StringComparer.Ordinal);

        public byte[]? Get(string key) =>
            _entries.TryGetValue(key, out byte[]? value) ? [.. value] : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _entries.TryRemove(key, out _);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _entries[key] = [.. value];

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDistributedCache : IDistributedCache
    {
        private static InvalidOperationException Unavailable() => new("Redis is unavailable.");

        public byte[]? Get(string key) => throw Unavailable();
        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => throw Unavailable();
        public void Refresh(string key) => throw Unavailable();
        public Task RefreshAsync(string key, CancellationToken token = default) => throw Unavailable();
        public void Remove(string key) => throw Unavailable();
        public Task RemoveAsync(string key, CancellationToken token = default) => throw Unavailable();
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => throw Unavailable();
        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) => throw Unavailable();
    }
}
