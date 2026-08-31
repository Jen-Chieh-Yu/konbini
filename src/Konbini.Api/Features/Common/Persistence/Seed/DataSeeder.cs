using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Common.Persistence.Seed;

/// <summary>
/// 啟動時建立 schema 並在空表時匯入種子資料。
/// TODO（練習項）：導入 EF Migrations 後，把 EnsureCreatedAsync 換成 MigrateAsync
///（EnsureCreated 建的資料庫無法再套用 migration）。本專案不正式上線、
/// 資料皆可重建，EnsureCreated 無實質風險；動 entity 結構前導入最順。
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Products.AnyAsync())
        {
            var csvPath = Path.Combine(AppContext.BaseDirectory,
                "Features", "Common", "Persistence", "Seed", "products.csv");
            if (File.Exists(csvPath))
            {
                var products = (await File.ReadAllLinesAsync(csvPath))
                    .Skip(1)
                    .Select(line => line.Split(','))
                    .Where(cols => cols.Length >= 4)
                    .Select(cols => new Product
                    {
                        Type = int.Parse(cols[0]),
                        Name = cols[1],
                        Price = int.Parse(cols[2]),
                        ImageUrl = cols[3],
                    });
                db.Products.AddRange(products);
            }
        }

        if (!await db.Cities.AnyAsync())
        {
            // 示範資料：完整縣市／行政區清單之後另行匯入
            db.Cities.AddRange(
                new City { CityCode = 1, CityName = "台北市" },
                new City { CityCode = 2, CityName = "新北市" },
                new City { CityCode = 3, CityName = "高雄市" });
            db.Districts.AddRange(
                new District { CityCode = 1, DistrictCode = 100, DistrictName = "中正區" },
                new District { CityCode = 1, DistrictCode = 104, DistrictName = "中山區" },
                new District { CityCode = 2, DistrictCode = 220, DistrictName = "板橋區" },
                new District { CityCode = 2, DistrictCode = 235, DistrictName = "中和區" },
                new District { CityCode = 3, DistrictCode = 800, DistrictName = "新興區" },
                new District { CityCode = 3, DistrictCode = 813, DistrictName = "左營區" });
        }

        await db.SaveChangesAsync();
    }
}
