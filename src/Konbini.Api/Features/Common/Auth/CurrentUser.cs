using System.Security.Claims;

namespace Konbini.Api.Features.Common.Auth;

public interface ICurrentUser
{
    int Id { get; }
}

/// <summary>
/// 從 JWT claims 取出目前使用者。
/// 只應在 RequireAuthorization 的端點鏈路上使用；未登入時取 Id 會擲例外。
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int Id
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? accessor.HttpContext?.User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
            return value is not null && int.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("目前請求沒有已驗證的使用者。");
        }
    }
}
