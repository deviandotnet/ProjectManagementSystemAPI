using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;

namespace PMS.API.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseScalarWithUi(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                // Expose raw OpenAPI JSON endpoint at /openapi/v1.json
                app.MapOpenApi();

                // Scalar API Reference UI at /scalar/v1
                app.MapScalarApiReference(options =>
                {
                    options.AddPreferredSecuritySchemes(JwtBearerDefaults.AuthenticationScheme);
                    options.AddHttpAuthentication(JwtBearerDefaults.AuthenticationScheme, auth =>
                    {
                        auth.Description = "Enter your JWT Bearer token";
                    });
                });
            }

            return app;
        }

        public static IApplicationBuilder UseSwaggerWithUi(this WebApplication app)
        {
            return app.UseScalarWithUi();
        }
    }
}
