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

            // ── Middleware Pipeline ────────────────────────────────────────────────
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();

            // ── Auto-discover and register all Minimal API endpoints ──────────────
            app.MapApiEndpoints();

            app.Run();
        }
    }
}
