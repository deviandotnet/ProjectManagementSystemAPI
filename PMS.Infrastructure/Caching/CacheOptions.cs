namespace PMS.Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Caching";

    public bool Enabled { get; init; } = true;
    public long MaximumPayloadBytes { get; init; } = 1024 * 1024;
    public int MaximumKeyLength { get; init; } = 1024;
    public bool ReportTagMetrics { get; init; }
    public RedisCacheOptions Redis { get; init; } = new();

    public sealed class RedisCacheOptions
    {
        public bool Enabled { get; init; }
        public bool Required { get; init; }
        public string? ConnectionString { get; init; }
    }
}
