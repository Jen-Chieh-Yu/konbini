using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Common.Persistence.Seed;

/// <summary>把 Admin:Email 的帳號設為管理員的結果，決定要記什麼 log。</summary>
public enum AdminPromotion
{
    NotFound,
    NotVerified,
    AlreadyAdmin,
    Promoted,
}

/// <summary>
/// 啟動時套用 EF Migrations，並在空表時匯入種子資料；設定了 Admin:Email 時，把那個帳號設為管理員。
/// </summary>
public static class DataSeeder
{
    /// <param name="adminEmail">Admin:Email 設定；空值表示不設管理員。</param>
    public static async Task SeedAsync(AppDbContext db, string? adminEmail, ILogger logger)
    {
        await db.Database.MigrateAsync();

        // 商品全被軟刪除時不該重新匯入種子
        if (!await db.Products.IgnoreQueryFilters().AnyAsync())
        {
            var csvPath = Path.Combine(AppContext.BaseDirectory,
                "Features", "Common", "Persistence", "Seed", "products.csv");
            if (File.Exists(csvPath))
            {
                var products = (await File.ReadAllLinesAsync(csvPath))
                    .Skip(1)
                    .Select(line => line.Split(','))
                    .Where(cols => cols.Length >= 5)
                    .Select(cols => new Product
                    {
                        Type = int.Parse(cols[0]),
                        Name = cols[1],
                        Price = int.Parse(cols[2]),
                        ImageUrl = cols[3],
                        Stock = int.Parse(cols[4]),
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

        await PromoteAdminAsync(db, adminEmail, logger);

        await db.SaveChangesAsync();
    }

    // 帳號要先從畫面註冊、完成 Email 驗證，下一次啟動 API 時才會設為管理員。密碼不放設定檔
    private static async Task PromoteAdminAsync(AppDbContext db, string? adminEmail, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            return;
        }

        var email = User.NormalizeEmail(adminEmail);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        switch (PromoteToAdmin(user))
        {
            case AdminPromotion.NotFound:
                logger.LogWarning("Admin:Email 指定的帳號還沒註冊，略過；註冊並完成 Email 驗證後重新啟動 API。");
                break;
            case AdminPromotion.NotVerified:
                logger.LogWarning("Admin:Email 指定的帳號還沒完成 Email 驗證，略過；完成驗證後重新啟動 API。");
                break;
            case AdminPromotion.Promoted:
                logger.LogInformation("已將 Admin:Email 指定的帳號（Id {UserId}）設為管理員。", user!.Id);
                break;
        }
    }

    /// <summary>
    /// 只升級已完成 Email 驗證的帳號：不檢查的話，誰先用這個 Email 註冊（收不到信也行），重新啟動後就是管理員。
    /// 只升不降：改掉 Admin:Email 不會撤銷原本的管理員（DD-AUT-004）。
    /// </summary>
    public static AdminPromotion PromoteToAdmin(User? user)
    {
        if (user is null)
        {
            return AdminPromotion.NotFound;
        }
        if (user.EmailVerifiedOn is null)
        {
            return AdminPromotion.NotVerified;
        }
        if (user.Role == UserRole.Admin)
        {
            return AdminPromotion.AlreadyAdmin;
        }

        user.Role = UserRole.Admin;
        return AdminPromotion.Promoted;
    }
}
