namespace PMS.API.Configuration;

internal sealed class CorsSecurityOptions
{
    internal const string SectionName = "Cors";

    public string AllowedOrigins { get; set; } = string.Empty;
}

internal sealed class ApiRateLimitingOptions
{
    internal const string SectionName = "RateLimiting";

    public GlobalRateLimitOptions Global { get; set; } = new();
    public AuthenticationRateLimitOptions Authentication { get; set; } = new();
    public ExportRateLimitOptions Export { get; set; } = new();
}

internal sealed class GlobalRateLimitOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowInSeconds { get; set; } = 60;
}

internal sealed class AuthenticationRateLimitOptions
{
    public int PermitLimit { get; set; } = 10;
    public int WindowInSeconds { get; set; } = 60;
    public int SegmentsPerWindow { get; set; } = 6;
}

internal sealed class ExportRateLimitOptions
{
    public int ConcurrencyLimit { get; set; } = 3;
    public int PermitLimit { get; set; } = 10;
    public int WindowInSeconds { get; set; } = 60;
}
