using Microsoft.AspNetCore.Diagnostics;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 全域例外處理：預期外錯誤一律回 500，不外洩內部訊息。
/// 請求格式錯誤（JSON 解析失敗、數字欄位送了字串）回 400；取不到登入身分回 401。
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

        if (exception is BadHttpRequestException badRequest)
        {
            // 屬於呼叫端的錯誤，不記成 Error
            logger.LogInformation("Bad request on {Path}: {Message}", httpContext.Request.Path, badRequest.Message);
            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                status = badRequest.StatusCode,
                title = "請求格式錯誤。",
            }, ct);
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
