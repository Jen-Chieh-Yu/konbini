using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Common.Persistence.Seed;
using Xunit;

namespace Konbini.Tests.Unit;

/// <summary>啟動時把 Admin:Email 的帳號設為管理員（DataSeeder.PromoteToAdmin）。</summary>
public class AdminPromotionTests
{
    [Fact]
    public void VerifiedCustomer_BecomesAdmin()
    {
        var user = new User { EmailVerifiedOn = DateTime.UtcNow };

        Assert.Equal(AdminPromotion.Promoted, DataSeeder.PromoteToAdmin(user));
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public void UnverifiedUser_StaysCustomer()
    {
        // 誰先用這個 Email 註冊都行，不檢查驗證的話，收不到信的人重新啟動後就是管理員
        var user = new User();

        Assert.Equal(AdminPromotion.NotVerified, DataSeeder.PromoteToAdmin(user));
        Assert.Equal(UserRole.Customer, user.Role);
    }

    [Fact]
    public void MissingUser_ReturnsNotFound()
    {
        Assert.Equal(AdminPromotion.NotFound, DataSeeder.PromoteToAdmin(null));
    }

    [Fact]
    public void ExistingAdmin_IsUnchanged()
    {
        var user = new User { EmailVerifiedOn = DateTime.UtcNow, Role = UserRole.Admin };

        Assert.Equal(AdminPromotion.AlreadyAdmin, DataSeeder.PromoteToAdmin(user));
        Assert.Equal(UserRole.Admin, user.Role);
    }
}
