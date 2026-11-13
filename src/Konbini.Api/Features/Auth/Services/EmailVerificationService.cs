using System.Net;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Email;
using Konbini.Api.Features.Common.Web;

namespace Konbini.Api.Features.Auth.Services;

/// <summary>
/// 註冊與重寄驗證信共用：產生 token、組信件、寄出。
/// 分兩步，因為信中的連結要等 token 寫進資料庫才查得到：
/// <c>Issue</c> → 呼叫端 <c>IUnitOfWork.SaveChangesAsync</c> → <c>SendAsync</c>。
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>產生新的 token 並加進 repository，回傳要放進信裡的原文。</summary>
    string Issue(User user);

    /// <summary>寄出驗證信；寄不出去時丟例外。</summary>
    Task SendAsync(User user, string rawToken, CancellationToken ct);
}

public sealed class EmailVerificationService(
    IEmailVerificationTokenRepository tokens,
    IEmailSender sender,
    AppOptions app,
    TimeProvider time) : IEmailVerificationService
{
    public const string Subject = "【Konbini】請驗證您的 Email";

    public string Issue(User user)
    {
        var (token, raw) = EmailVerificationToken.Issue(user, time.GetUtcNow().UtcDateTime);
        tokens.Add(token);
        return raw;
    }

    public Task SendAsync(User user, string rawToken, CancellationToken ct)
    {
        // token 是 base64url（A–Z、a–z、0–9、-、_），放進網址不需要再編碼
        var link = $"{app.ClientBaseUrl}/verify-email?token={rawToken}";
        var hours = (int)EmailVerificationToken.Lifetime.TotalHours;
        var name = WebUtility.HtmlEncode(user.Name);
        var href = WebUtility.HtmlEncode(link);

        var html = $"""
            <p>{name} 您好：</p>
            <p>感謝您註冊 Konbini。請在 {hours} 小時內點下方按鈕完成 Email 驗證，驗證後就能登入。</p>
            <p><a href="{href}" style="display:inline-block;padding:12px 24px;background:#d33a2c;color:#ffffff;text-decoration:none;border-radius:4px;font-weight:bold;">驗證 Email</a></p>
            <p>按鈕無法點選時，請把下面的網址複製到瀏覽器開啟：<br>{href}</p>
            <p>如果不是您本人註冊，請忽略這封信。</p>
            """;
        var text = $"""
            {user.Name} 您好：

            感謝您註冊 Konbini。請在 {hours} 小時內開啟下面的網址完成 Email 驗證，驗證後就能登入。

            {link}

            如果不是您本人註冊，請忽略這封信。
            """;

        return sender.SendAsync(new EmailMessage(user.Email, Subject, html, text), ct);
    }
}
