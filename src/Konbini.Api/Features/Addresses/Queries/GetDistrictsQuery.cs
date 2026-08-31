using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Addresses.Queries;

public record GetDistrictsQuery(int CityCode);

public class GetDistrictsHandler(AppDbContext db)
    : IQueryHandler<GetDistrictsQuery, List<DistrictDto>>
{
    public async Task<List<DistrictDto>> Handle(GetDistrictsQuery query, CancellationToken ct)
        => await db.Districts.AsNoTracking()
            .Where(d => d.CityCode == query.CityCode)
            .OrderBy(d => d.DistrictCode)
            .Select(d => new DistrictDto(d.DistrictCode, d.DistrictName))
            .ToListAsync(ct);
}
