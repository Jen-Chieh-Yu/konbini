using System.Diagnostics;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 把可預期的失敗轉成 ProblemDetails（RFC 9457）：<c>{ type, title, status, errors, traceId }</c>，
/// 與 GlobalExceptionHandler 的 400／500 同一種格式。errors 的值是單一字串（一欄一則訊息），
/// 不用 ASP.NET ValidationProblemDetails 的字串陣列。
/// </summary>
public static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        var (status, title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "輸入資料有誤。"),
            ErrorType.Unauthorized => (StatusCodes.Status401Unauthorized, "身分驗證失敗。"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "沒有權限執行這個操作。"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "找不到資料。"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "與目前的資料狀態衝突。"),
            _ => throw new UnreachableException($"未對應的錯誤類型：{error.Type}"),
        };

        return Results.Problem(
            statusCode: status,
            title: title,
            extensions: new Dictionary<string, object?> { ["errors"] = error.Errors });
    }
}
