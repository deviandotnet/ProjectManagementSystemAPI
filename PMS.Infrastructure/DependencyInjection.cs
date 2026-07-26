using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Abstractions.Data;
using PMS.Infrastructure.Data;
using PMS.Infrastructure.Interceptors;
using PMS.Infrastructure.Repository;

namespace PMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
        {
            services.AddSingleton<AuditInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
                
                var provider = configuration["DbProvider"] ?? "SqlServer";
                var connectionString = configuration.GetConnectionString("DbConnection");

                if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) || 
                    provider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseNpgsql(connectionString);
                }
                else
                {
                    options.UseSqlServer(connectionString);
                }
            });

            // Register the interface so handlers can inject IApplicationDbContext
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<PMS.Application.Abstractions.Authentication.IPasswordHasher, Authentication.PasswordHasher>();

            return services;
        }
    }
}
