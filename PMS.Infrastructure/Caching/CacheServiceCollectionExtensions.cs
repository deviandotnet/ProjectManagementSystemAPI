using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PMS.Application.Abstractions.Caching;

namespace PMS.Infrastructure.Caching;

internal static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        CacheOptions options = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        if (options.MaximumPayloadBytes <= 0)
        {
            throw new InvalidOperationException("Caching:MaximumPayloadBytes must be greater than zero.");
        }

        if (options.MaximumKeyLength <= 0)
        {
            throw new InvalidOperationException("Caching:MaximumKeyLength must be greater than zero.");
        }

        bool useRedis = options.Redis.Enabled || options.Redis.Required;

        if (useRedis)
        {
            string? connectionString = string.IsNullOrWhiteSpace(options.Redis.ConnectionString)
                ? configuration.GetConnectionString("Redis")
                : options.Redis.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Redis caching is enabled, but no Redis connection string is configured.");
            }

            services.AddStackExchangeRedisCache(redis =>
            {
                redis.Configuration = connectionString;
                redis.InstanceName = "pms:";
            });

            services.AddHealthChecks()
                .AddCheck<RedisCacheHealthCheck>(
                    "Redis Cache",
                    failureStatus: HealthStatus.Degraded,
                    tags: ["ready", "cache"]);
        }

        services.AddHybridCache(hybrid =>
        {
            hybrid.MaximumPayloadBytes = options.MaximumPayloadBytes;
            hybrid.MaximumKeyLength = options.MaximumKeyLength;
            hybrid.ReportTagMetrics = options.ReportTagMetrics;
        });
        services.AddSingleton(options);
        services.AddSingleton<IApplicationCache, HybridApplicationCache>();

        return services;
    }
}
