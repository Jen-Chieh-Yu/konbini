using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Addresses.Repositories;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Addresses.Queries;

public record GetDistrictsQuery(int CityCode);

public class GetDistrictsHandler(IAddressRepository addresses)
    : IQueryHandler<GetDistrictsQuery, List<DistrictDto>>
{
    public async Task<List<DistrictDto>> Handle(GetDistrictsQuery query, CancellationToken ct)
        => await addresses.GetDistrictsAsync(query.CityCode, ct);
}
