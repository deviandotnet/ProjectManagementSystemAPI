using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetProjectById;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class GetProjectById : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{id:guid}", async (
            Guid id,
            IQueryHandler<GetProjectByIdQuery, ProjectResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectByIdQuery(id);

            Result<ProjectResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                project => Results.Ok(project),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithTags(Tags.Projects);
    }
}
