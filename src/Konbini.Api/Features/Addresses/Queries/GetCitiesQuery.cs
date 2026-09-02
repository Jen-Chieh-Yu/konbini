using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Addresses.Repositories;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Addresses.Queries;

public record GetCitiesQuery;

public class GetCitiesHandler(IAddressRepository addresses)
    : IQueryHandler<GetCitiesQuery, List<CityDto>>
{
    public async Task<List<CityDto>> Handle(GetCitiesQuery query, CancellationToken ct)
        => await addresses.GetCitiesAsync(ct);
}
