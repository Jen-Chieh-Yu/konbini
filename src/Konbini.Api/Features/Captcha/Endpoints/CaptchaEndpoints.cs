using Konbini.Api.Features.Captcha.Commands;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Captcha.Endpoints;

public class CaptchaEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/captchas").WithTags("Captcha");

        // 回 200 加整個內容，不回 201＋Location：沒有讀取驗證碼的端點，圖片只在建立時給一次
        group.MapPost("/", async (
                HttpContext http,
                ICommandHandler<CreateCaptchaCommand, CaptchaResponse> handler,
                CancellationToken ct)
            =>
            {
                // 每次都是新的一張，不能被瀏覽器或代理快取
                http.Response.Headers.CacheControl = "no-store";
                return Results.Ok(await handler.Handle(new(), ct));
            })
            .Produces<CaptchaResponse>();
    }
}
