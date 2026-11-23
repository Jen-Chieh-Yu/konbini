using System.Security.Claims;

namespace Konbini.Api.Features.Common.Auth;

public interface ICurrentUser
{
    int Id { get; }

    /// <summary>未登入時為 null，給註冊、種子資料這類沒有登入身分的流程使用。</summary>
    int? IdOrNull { get; }
}

/// <summary>
/// 從 JWT claims 取出目前使用者。
/// 只應在 RequireAuthorization 的端點鏈路上使用；未登入時取 Id 會擲例外。
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int Id => IdOrNull
        ?? throw new UnauthorizedAccessException("目前請求沒有已驗證的使用者。");

    public int? IdOrNull => accessor.HttpContext?.User is { } user ? ParseId(user) : null;

    /// <summary>從 claims 取使用者 Id（sub）；沒有或不是整數時回 null。授權的 policy handler 也用這個。</summary>
    public static int? ParseId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? user.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        return int.TryParse(value, out var id) ? id : null;
    }
}
