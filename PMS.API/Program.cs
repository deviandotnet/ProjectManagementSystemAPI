using DotNetEnv;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using PMS.API.Extensions;
using PMS.Application;
using PMS.Infrastructure;
using PMS.Infrastructure.Database;

namespace PMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.TraversePath().Load(); // Load environment variables from .env file (traversing parent paths)

            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();

            // Configure Forwarded Headers for Proxy (Render)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
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

            // Application (Handlers + Validators)
            builder.Services.AddApplication();

            // JSON Options (Enum support)
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

            // API Endpoints
            builder.Services.RegisterApiEndpointsFromAssembly(typeof(Program).Assembly);

            // Infrastructure (DbContext + AuditInterceptor)
            builder.Services.AddInfrastructure(builder.Configuration);

            // API Services 
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApiWithAuth();

            var app = builder.Build();

            // Use Forwarded Headers first before any other middleware
            app.UseForwardedHeaders();

            // Auto-Migrate Database on Cloud Startup
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (dbContext.Database.IsRelational())
                {
                    dbContext.Database.Migrate();
                }
            }

            // Middleware Pipeline
            app.UseCors();

            // Auto-discover and register all Minimal API endpoints
            app.MapApiEndpoints();

            app.UseScalarWithUi();
            app.UseHttpsRedirection();

            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseScalarWithUi();
            //    app.UseHttpsRedirection();
            //}

            app.UseAuthentication();
            app.UseAuthorization();

            app.Run();
        }
    }
}
