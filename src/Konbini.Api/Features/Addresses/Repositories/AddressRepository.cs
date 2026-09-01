using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Addresses.Repositories;

public interface IAddressRepository : IRepository
{
    Task<List<CityDto>> GetCitiesAsync(CancellationToken ct);

    Task<List<DistrictDto>> GetDistrictsAsync(int cityCode, CancellationToken ct);

    /// <summary>取縣市名稱；查無回 null（呼叫端據此判定代碼是否有效）。</summary>
    Task<string?> GetCityNameAsync(int cityCode, CancellationToken ct);

    /// <summary>取行政區名稱（需隸屬該縣市）；查無回 null。</summary>
    Task<string?> GetDistrictNameAsync(int cityCode, int districtCode, CancellationToken ct);
}

public sealed class AddressRepository(AppDbContext db) : IAddressRepository
{
    public async Task<List<CityDto>> GetCitiesAsync(CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .OrderBy(c => c.CityCode)
            .Select(c => new CityDto(c.CityCode, c.CityName))
            .ToListAsync(ct);

    public async Task<List<DistrictDto>> GetDistrictsAsync(int cityCode, CancellationToken ct)
        => await db.Districts.AsNoTracking()
            .Where(d => d.CityCode == cityCode)
            .OrderBy(d => d.DistrictCode)
            .Select(d => new DistrictDto(d.DistrictCode, d.DistrictName))
            .ToListAsync(ct);

    public async Task<string?> GetCityNameAsync(int cityCode, CancellationToken ct)
        => await db.Cities.AsNoTracking()
            .Where(c => c.CityCode == cityCode)
            .Select(c => c.CityName)
            .FirstOrDefaultAsync(ct);

    public async Task<string?> GetDistrictNameAsync(int cityCode, int districtCode, CancellationToken ct)
        => await db.Districts.AsNoTracking()
            .Where(d => d.DistrictCode == districtCode && d.CityCode == cityCode)
            .Select(d => d.DistrictName)
            .FirstOrDefaultAsync(ct);
}
