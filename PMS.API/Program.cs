using Microsoft.EntityFrameworkCore;
using PMS.Application;
using PMS.Application.Extensions;
using PMS.Infrastructure;
using Scalar.AspNetCore;

namespace PMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ── Application (Handlers + Validators + Endpoints) ────────────────────
            builder.Services.AddApplication();

            // ── Infrastructure (DbContext + AuditInterceptor) ──────────────────────
            builder.Services.AddInfrastructure(builder.Configuration);

            // ── API Services ───────────────────────────────────────────────────────
            builder.Services.AddAuthorization();
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // ── Auto-Migrate Database on Cloud Startup ─────────────────────────────
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<PMS.Infrastructure.Data.ApplicationDbContext>();
                dbContext.Database.Migrate();
            }

            // ── Middleware Pipeline ────────────────────────────────────────────────
            app.MapOpenApi();
            app.MapScalarApiReference();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // ── Auto-discover and register all Minimal API endpoints ──────────────
            app.MapApiEndpoints();

            app.Run();
        }
    }
}
