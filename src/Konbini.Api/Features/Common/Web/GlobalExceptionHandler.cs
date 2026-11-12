using Microsoft.AspNetCore.Diagnostics;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 全域例外處理：預期外錯誤一律回 500，不外洩內部訊息。
/// 請求格式錯誤（JSON 解析失敗、數字欄位送了字串）回 400；取不到登入身分回 401。
/// 預期內錯誤（驗證失敗等）由各 handler 以 Result 回傳，不走例外。
/// 400／500 與 Result 的失敗（ErrorResults）同樣是 ProblemDetails。
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
            await Results.Problem(statusCode: badRequest.StatusCode, title: "請求格式錯誤。")
                .ExecuteAsync(httpContext);
            return true;
        }

        logger.LogError(exception, "Unhandled exception on {Path}", httpContext.Request.Path);
        await Results.Problem(statusCode: StatusCodes.Status500InternalServerError, title: "伺服器發生錯誤，請稍後再試。")
            .ExecuteAsync(httpContext);
        return true;
    }
}
