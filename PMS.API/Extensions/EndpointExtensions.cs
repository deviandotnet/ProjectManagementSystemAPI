using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PMS.Application.Abstractions;

namespace PMS.API.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection RegisterApiEndpointsFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        var endpointTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(typeof(IApiEndpoint)) && t is { IsClass: true, IsAbstract: false, IsInterface: false });

        var serviceDescriptors = endpointTypes
            .Select(type => ServiceDescriptor.Transient(typeof(IApiEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);
        return services;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        var endpoints = app.Services.GetRequiredService<IEnumerable<IApiEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}
