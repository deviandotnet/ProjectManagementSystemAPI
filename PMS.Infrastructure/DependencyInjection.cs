using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Abstractions.Data;
using PMS.Infrastructure.Database;
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
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"));

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
