using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.ExportProjectExcel;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class ExportProjectExcel : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/export/excel", async (
            Guid projectId,
            IQueryHandler<ExportProjectExcelQuery, ExportProjectExcelResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new ExportProjectExcelQuery(projectId);

            Result<ExportProjectExcelResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                response => Results.File(
                    response.FileContent,
                    response.ContentType,
                    response.FileName),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Export Project to Excel")
        .WithDescription("Downloads a formatted .xlsx workbook containing the full action items data table and a visual Gantt timeline view with status-colored cells.")
        .WithTags(Tags.Export);
    }
}
