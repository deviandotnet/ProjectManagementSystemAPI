using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Holidays.DeleteHoliday;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Holidays;

internal sealed class DeleteHoliday : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/holidays/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteHolidayCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteHolidayCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Delete Holiday")
        .WithDescription("Permanently deletes a holiday from the global calendar. Requires SystemAdmin.")
        .WithTags(Tags.Holidays);
    }
}
