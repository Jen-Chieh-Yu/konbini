namespace Konbini.Api.Features.Captcha.Models;

/// <summary>
/// 驗證碼的設定（<c>Captcha:*</c>）。啟動時檢查，同 <see cref="Common.Email.EmailOptions"/>。
/// </summary>
public sealed class CaptchaOptions
{
    /// <summary>效期。登入表單短，2 分鐘夠用；前端在過期時自動換一張（DD-AUT-003）。</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// 開發環境的固定答案：設定時，這個答案一律通過，Scalar、PowerShell 測試登入不用看圖。
    /// 只在 Development 有效；其他環境設了就啟動失敗，避免部署時帶著後門上線。
    /// </summary>
    public string? DevelopmentAnswer { get; init; }

    public static CaptchaOptions FromConfiguration(IConfiguration config, IHostEnvironment environment)
    {
        var answer = config["Captcha:DevelopmentAnswer"];
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new CaptchaOptions();
        }
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                $"Captcha:DevelopmentAnswer 只能在 Development 環境設定（目前是 {environment.EnvironmentName}），請移除這個設定（docs/configuration.md）。");
        }
        return new CaptchaOptions { DevelopmentAnswer = CaptchaCode.Normalize(answer) };
    }
}
