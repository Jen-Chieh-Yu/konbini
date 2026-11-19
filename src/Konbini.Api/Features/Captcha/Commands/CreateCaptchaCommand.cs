using Konbini.Api.Features.Captcha.Models;
using Konbini.Api.Features.Captcha.Services;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Captcha.Commands;

public record CreateCaptchaCommand;

/// <param name="Id">送出受保護的請求時放在 <c>X-Captcha-Id</c> header。</param>
/// <param name="Image">PNG 的 data URL，前端直接放進 img 的 src。</param>
/// <param name="ExpiresIn">幾秒後過期，前端到時間自動換一張。</param>
public record CaptchaResponse(string Id, string Image, int ExpiresIn);

/// <summary>產生一張驗證碼：答案存進 <see cref="ICaptchaStore"/>，只把圖片與 id 回給前端。</summary>
public class CreateCaptchaHandler(ICaptchaStore store, ICaptchaImageRenderer renderer)
    : ICommandHandler<CreateCaptchaCommand, CaptchaResponse>
{
    public Task<CaptchaResponse> Handle(CreateCaptchaCommand command, CancellationToken ct)
    {
        var code = CaptchaCode.Generate();
        var image = "data:image/png;base64," + Convert.ToBase64String(renderer.RenderPng(code));
        var id = store.Save(code);
        return Task.FromResult(new CaptchaResponse(id, image, (int)CaptchaOptions.Lifetime.TotalSeconds));
    }
}
