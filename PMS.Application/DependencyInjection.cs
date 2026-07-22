using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Abstractions;
using PMS.Application.Extensions;
using PMS.Application.Pipelines;
using System.Reflection;

namespace PMS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddValidatorsFromAssembly(assembly); // Register FluentValidation validators from the assembly
            services.AddHandlersFromAssembly(assembly); // Register handlers from the assembly
            services.RegisterApiEndpointsFromAssembly(assembly); // Register API endpoints from the assembly
            return services;
        }

        private static IServiceCollection AddHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
        {
            var handlerTypes = assembly.GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                !t.ContainsGenericParameters)
            .ToList();

            foreach (var implementation in handlerTypes)
            {
                var handlerInterfaces = implementation
                .GetInterfaces()
                .Where(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IHandler<,>));

                foreach (var handlerInterface in handlerInterfaces)
                {
                    services.AddScoped(handlerInterface, implementation);
                }
            }

            services.Decorate(typeof(IHandler<,>), typeof(ValidationDecorator<,>));
            services.Decorate(typeof(IHandler<,>), typeof(LoggingDecorator<,>));

            return services;
        }
    }
}
