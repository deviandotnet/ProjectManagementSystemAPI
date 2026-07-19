using PMS.Infrastructure;
using Scalar.AspNetCore;

namespace PMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

            app.Run();
        }
    }
}
