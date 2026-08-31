using Microsoft.AspNetCore.Diagnostics;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 全域例外處理：預期外錯誤一律回 500 + ProblemDetails，不外洩內部訊息。
/// 預期內錯誤（驗證失敗等）由各 handler 以結果物件回傳，不走例外。
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return true;
        }

        logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            status = 500,
            title = "伺服器發生錯誤，請稍後再試。",
        }, ct);
        return true;
    }
}
