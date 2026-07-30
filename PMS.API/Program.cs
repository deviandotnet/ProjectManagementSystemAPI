using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PMS.API.Extensions;
using PMS.Application;
using PMS.Infrastructure;
using PMS.Infrastructure.Database;
using Scalar.AspNetCore;

namespace PMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (File.Exists(".env"))
            {
                try
                {
                    DotNetEnv.Env.Load();
                }
                catch
                {
                    // Ignore error if .env loading is not available
                }
            }

            var builder = WebApplication.CreateBuilder(args);

            // Configure Forwarded Headers for Proxy (Render)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // CORS Policy 
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Application (Handlers + Validators + Endpoints)
            builder.Services.AddApplication();

            // Infrastructure (DbContext + AuditInterceptor)
            builder.Services.AddInfrastructure(builder.Configuration);

            // API Services 
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Use Forwarded Headers first before any other middleware
            app.UseForwardedHeaders();

            // Auto-Migrate Database on Cloud Startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            // Middleware Pipeline
            app.UseCors();

            #region //only for development, remove in production (wrap in if (app.Environment.IsDevelopment()) if needed) just exposed for production testing purposes
            app.MapOpenApi();
            app.MapScalarApiReference();
            #endregion

            if (app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
                app.MapOpenApi();
                app.MapScalarApiReference();
            }
            app.UseAuthorization();

            // Auto-discover and register all Minimal API endpoints
            app.MapApiEndpoints();

            app.Run();
        }
    }
}
