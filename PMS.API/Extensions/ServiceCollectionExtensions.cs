using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace PMS.API.Extensions
{
    internal static class ServiceCollectionExtensions
    {
        private const string BearerSchemeId = JwtBearerDefaults.AuthenticationScheme;

        /// <summary>
        /// Registers OpenAPI document generation with a JWT Bearer security scheme —
        /// using the native Microsoft.AspNetCore.OpenApi document-transformer pipeline.
        /// </summary>
        internal static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services)
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Components ??= new OpenApiComponents();

                    // Define the JWT Bearer security scheme
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    document.Components.SecuritySchemes[BearerSchemeId] = new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Description = "Enter your JWT Bearer token",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    };

                    // Apply the security scheme globally across all endpoints
                    var schemeRef = new OpenApiSecuritySchemeReference(BearerSchemeId);
                    var requirement = new OpenApiSecurityRequirement
                    {
                        [schemeRef] = []
                    };

                    document.Security ??= [];
                    document.Security.Add(requirement);

                    return Task.CompletedTask;
                });
            });

            return services;
        }
    }
}
