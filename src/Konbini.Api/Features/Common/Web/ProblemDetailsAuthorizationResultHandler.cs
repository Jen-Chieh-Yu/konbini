using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// policy 不通過（已登入、但不是管理員）時回 403 ProblemDetails，和 Handler 回的錯誤同一種格式；
/// 預設的 403 沒有內容，前端讀不到 errors。其他情況（通過、401）照預設處理。
/// </summary>
public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(
        RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            await AuthErrors.Forbidden.ToProblem().ExecuteAsync(context);
            return;
        }

        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
