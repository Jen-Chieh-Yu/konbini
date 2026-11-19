using Konbini.Api.Features.Captcha.Errors;
using Konbini.Api.Features.Captcha.Models;
using Konbini.Api.Features.Captcha.Services;
using Konbini.Api.Features.Common.Web;
using Microsoft.OpenApi;

namespace Konbini.Api.Features.Captcha.Endpoints;

/// <summary>
/// 檢查 <c>X-Captcha-Id</c>／<c>X-Captcha-Answer</c> header，沒通過就不進 Handler。
/// 放 endpoint filter 不放 Handler（CLAUDE.md §2）：驗證碼是 HTTP 請求的事，Handler 與 request 都不用改。
/// 先於 Handler 執行，所以驗證碼沒過時看不出密碼對不對、帳號驗證了沒有。
/// </summary>
public sealed class CaptchaEndpointFilter(ICaptchaStore store, CaptchaOptions options) : IEndpointFilter
{
    public const string IdHeader = "X-Captcha-Id";
    public const string AnswerHeader = "X-Captcha-Answer";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var headers = context.HttpContext.Request.Headers;
        var answer = headers[AnswerHeader].ToString();
        if (string.IsNullOrWhiteSpace(answer))
        {
            return CaptchaErrors.Required.ToProblem();
        }
        if (options.DevelopmentAnswer is { } devAnswer && CaptchaCode.Normalize(answer) == devAnswer)
        {
            return await next(context);
        }
        if (!store.TryConsume(headers[IdHeader].ToString(), answer))
        {
            return CaptchaErrors.Invalid.ToProblem();
        }
        return await next(context);
    }
}

public static class CaptchaEndpointExtensions
{
    /// <summary>這個端點要先通過驗證碼。OpenAPI 文件同時列出兩個 header，Scalar 上才知道要填。</summary>
    public static RouteHandlerBuilder RequireCaptcha(this RouteHandlerBuilder builder) =>
        builder
            .AddEndpointFilter<CaptchaEndpointFilter>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .AddOpenApiOperationTransformer((operation, _, _) =>
            {
                operation.Parameters ??= [];
                operation.Parameters.Add(Header(CaptchaEndpointFilter.IdHeader,
                    "POST /api/captchas 回傳的 id（開發環境用固定答案時可以省略）", required: false));
                operation.Parameters.Add(Header(CaptchaEndpointFilter.AnswerHeader, "圖中的字元，不分大小寫", required: true));
                return Task.CompletedTask;
            });

    private static OpenApiParameter Header(string name, string description, bool required) => new()
    {
        Name = name,
        In = ParameterLocation.Header,
        Required = required,
        Description = description,
        Schema = new OpenApiSchema { Type = JsonSchemaType.String },
    };
}
