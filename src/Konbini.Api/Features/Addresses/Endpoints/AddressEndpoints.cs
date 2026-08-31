using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Addresses.Queries;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Addresses.Endpoints;

public class AddressEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/addresses").WithTags("Addresses");

        group.MapGet("/cities", async (
                IQueryHandler<GetCitiesQuery, List<CityDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(), ct)));

        group.MapGet("/cities/{cityCode:int}/districts", async (
                int cityCode,
                IQueryHandler<GetDistrictsQuery, List<DistrictDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(cityCode), ct)));
    }
}
