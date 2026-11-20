using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace Konbini.Api.Features.Common.Auth;

/// <summary>
/// 授權 policy 的名稱。目前只有管理員一種：角色只有兩種，依能力拆成多個 policy 只會全部對到 Admin。
/// 出現第二種後台角色（例：只能出貨）時再拆（DD-AUT-004）。
/// </summary>
public static class Policies
{
    public const string Admin = "Admin";
}

/// <summary>「目前使用者是管理員」的條件，由 <see cref="AdminAuthorizationHandler"/> 判斷。</summary>
public sealed class AdminRequirement : IAuthorizationRequirement;

/// <summary>
/// 從資料庫查目前使用者的角色。token 只放使用者 Id，不放角色：放進 token 的話，降權要等 token 過期（8 小時）才生效。
/// 只有掛了 Admin policy 的端點會執行，前台的請求不多查一次資料庫。
/// 帳號不存在或已軟刪除時查不到角色，一樣不通過。
/// </summary>
public sealed class AdminAuthorizationHandler(IUserRepository users) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        // 未登入時沒有使用者 Id：policy 的 RequireAuthenticatedUser 已經不通過，回 401
        if (CurrentUser.ParseId(context.User) is not { } userId)
        {
            return;
        }

        var ct = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;
        if (await users.GetRoleAsync(userId, ct) == UserRole.Admin)
        {
            context.Succeed(requirement);
        }
    }
}
