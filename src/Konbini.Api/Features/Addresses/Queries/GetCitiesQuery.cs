using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Addresses.Queries;

public record GetCitiesQuery;

public class GetCitiesHandler(AppDbContext db)
    : IQueryHandler<GetCitiesQuery, List<CityDto>>
{
    public async Task<List<CityDto>> Handle(GetCitiesQuery query, CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.CityCode)
            .Select(c => new CityDto(c.CityCode, c.CityName))
            .ToListAsync(ct);
}
