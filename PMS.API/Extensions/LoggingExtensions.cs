using Microsoft.AspNetCore.Builder;
using Serilog;

namespace PMS.API.Extensions;

/// <summary>
/// Encapsulates Serilog configuration and HTTP request logging middleware.
/// Keeps Program.cs minimal while preserving Clean Architecture layer boundaries.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Connects Serilog to the application host, reading sinks and levels from configuration.
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfiguration) =>
        {
            loggerConfiguration.ReadFrom.Configuration(context.Configuration);
        });

        return builder;
    }

    /// <summary>
    /// Enables Serilog structured HTTP request logging to log method, route, status code, and response time.
    /// </summary>
    public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        });
    }
}
