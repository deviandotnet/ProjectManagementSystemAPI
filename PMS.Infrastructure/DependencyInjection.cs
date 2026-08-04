using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Infrastructure.Authentication;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.Infrastructure.Repository;
using PMS.Infrastructure.Time;
using PMS.SharedKernel;
using System.Text;

namespace PMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<AuditInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                if (!options.IsConfigured)
                {
                    var connectionString = configuration.GetConnectionString("DbConnection")
                        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DbConnection")
                        ?? throw new InvalidOperationException("Database connection string is not configured.");

                    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
                    options.UseNpgsql(connectionString);
                }
            });

            // Register the interface so handlers can inject IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<ITokenProvider, TokenProvider>();

            // Configure JWT Bearer Authentication
            string rawSecret = configuration["Jwt:Secret"] ?? string.Empty;
            string secretKey = string.IsNullOrWhiteSpace(rawSecret)
                ? "super_secret_default_key_at_least_32_bytes_long!"
                : rawSecret;

            string rawIssuer = configuration["Jwt:Issuer"] ?? string.Empty;
            string issuer = string.IsNullOrWhiteSpace(rawIssuer) ? "PMS" : rawIssuer;

            string rawAudience = configuration["Jwt:Audience"] ?? string.Empty;
            string audience = string.IsNullOrWhiteSpace(rawAudience) ? "PMS" : rawAudience;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            return services;
        }
    }
}
