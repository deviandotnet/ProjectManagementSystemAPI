using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Holidays.UpdateHoliday;
using PMS.Domain.HolidayCalendars;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Holidays;

internal sealed class UpdateHoliday : IApiEndpoint
{
    public sealed record UpdateHolidayRequest(
        DateOnly HolidayDate,
        string Name,
        int Type,
        bool IsRecurringAnnually = false,
        int? Year = null
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/holidays/{id:guid}", async (
            Guid id,
            UpdateHolidayRequest request,
            ICommandHandler<UpdateHolidayCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateHolidayCommand(
                id,
                request.HolidayDate,
                request.Name,
                (HolidayType)request.Type,
                request.IsRecurringAnnually,
                request.Year);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Update Holiday")
        .WithDescription("Updates an existing holiday in the global calendar. Requires SystemAdmin.")
        .WithTags(Tags.Holidays);
    }
}
