namespace Konbini.Api.Features.Addresses.Models;

public class City
{
    public int CityCode { get; set; }
    public string CityName { get; set; } = string.Empty;
}

public class District
{
    public int CityCode { get; set; }
    public int DistrictCode { get; set; }
    public string DistrictName { get; set; } = string.Empty;
}
