using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using PMS.API.Configuration;

namespace PMS.API.Extensions;

internal static class SecurityExtensions
{
    private const string RetryAfterHeader = "Retry-After";

    internal static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CorsSecurityOptions>()
            .Bind(configuration.GetSection(CorsSecurityOptions.SectionName))
            .Validate(
                options => HasValidOrigins(options.AllowedOrigins),
                "CORS origins must use scheme://host[:port] with no path or trailing slash.")
            .ValidateOnStart();

        services.AddOptions<ApiRateLimitingOptions>()
            .Bind(configuration.GetSection(ApiRateLimitingOptions.SectionName))
            .Validate(HasValidRateLimits, "All rate-limit values must be greater than zero.")
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSecurityOptions>>((options, securityOptions) =>
            {
                string[] allowedOrigins = ParseAllowedOrigins(securityOptions.Value.AllowedOrigins);
                options.AddDefaultPolicy(policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins);
                    }

                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithExposedHeaders(RetryAfterHeader);
                });
            });

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<ApiRateLimitingOptions>>((options, rateLimitOptions) =>
            {
                ApiRateLimitingOptions rateLimits = rateLimitOptions.Value;
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        GetUserOrIpPartitionKey(context),
                        _ => CreateFixedWindowOptions(
                            rateLimits.Global.PermitLimit,
                            rateLimits.Global.WindowInSeconds)));

                options.AddPolicy(SecurityPolicies.Authentication, context =>
                    RateLimitPartition.GetSlidingWindowLimiter(
                        GetIpPartitionKey(context),
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = rateLimits.Authentication.PermitLimit,
                            Window = TimeSpan.FromSeconds(rateLimits.Authentication.WindowInSeconds),
                            SegmentsPerWindow = rateLimits.Authentication.SegmentsPerWindow,
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            AutoReplenishment = true
                        }));

                options.AddPolicy(SecurityPolicies.Export, context =>
                    RateLimitPartition.Get(
                        GetUserOrIpPartitionKey(context),
                        _ => RateLimiter.CreateChained(
                            new ConcurrencyLimiter(new ConcurrencyLimiterOptions
                            {
                                PermitLimit = rateLimits.Export.ConcurrencyLimit,
                                QueueLimit = 0,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                            new FixedWindowRateLimiter(CreateFixedWindowOptions(
                                rateLimits.Export.PermitLimit,
                                rateLimits.Export.WindowInSeconds)))));

                options.OnRejected = async (context, _) =>
                {
                    TimeSpan retryAfter = context.Lease.TryGetMetadata(
                        MetadataName.RetryAfter,
                        out TimeSpan retryAfterMetadata)
                        ? retryAfterMetadata
                        : TimeSpan.FromSeconds(1);

                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(
                        1,
                        (int)Math.Ceiling(retryAfter.TotalSeconds))
                        .ToString(CultureInfo.InvariantCulture);

                    await Results.Problem(
                        statusCode: StatusCodes.Status429TooManyRequests,
                        title: "Too Many Requests",
                        detail: "The request rate limit has been exceeded. Retry after the indicated delay.")
                        .ExecuteAsync(context.HttpContext);
                };
            });

        return services;
    }

    private static string[] ParseAllowedOrigins(string? configuredOrigins)
    {
        if (string.IsNullOrWhiteSpace(configuredOrigins))
        {
            return [];
        }

        string[] origins = configuredOrigins
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (string origin in origins)
        {
            if (origin.EndsWith("/", StringComparison.Ordinal) ||
                !Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                string.IsNullOrWhiteSpace(uri.Host) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                uri.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidOperationException(
                    $"Invalid CORS origin '{origin}'. Use scheme://host[:port] with no path or trailing slash.");
            }
        }

        return origins;
    }

    private static bool HasValidOrigins(string? configuredOrigins)
    {
        try
        {
            ParseAllowedOrigins(configuredOrigins);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasValidRateLimits(ApiRateLimitingOptions options) =>
        options.Global.PermitLimit > 0 &&
        options.Global.WindowInSeconds > 0 &&
        options.Authentication.PermitLimit > 0 &&
        options.Authentication.WindowInSeconds > 0 &&
        options.Authentication.SegmentsPerWindow > 0 &&
        options.Export.ConcurrencyLimit > 0 &&
        options.Export.PermitLimit > 0 &&
        options.Export.WindowInSeconds > 0;

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(
        int permitLimit,
        int windowInSeconds) => new()
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowInSeconds),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };

    private static string GetUserOrIpPartitionKey(HttpContext context)
    {
        string? userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        return context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(userId)
            ? $"user:{userId}"
            : GetIpPartitionKey(context);
    }

    private static string GetIpPartitionKey(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

}
