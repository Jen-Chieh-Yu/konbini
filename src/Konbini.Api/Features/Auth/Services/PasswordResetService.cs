using System.Net;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Email;
using Konbini.Api.Features.Common.Web;

namespace Konbini.Api.Features.Auth.Services;

/// <summary>
/// 申請重設與執行重設共用：產生 token、寄重設信、寄「密碼已變更」通知。
/// 和 <see cref="EmailVerificationService"/> 一樣分兩步，信中的連結要等 token 寫進資料庫才查得到：
/// <c>Issue</c> → 呼叫端 <c>IUnitOfWork.SaveChangesAsync</c> → <c>SendResetLinkAsync</c>。
/// </summary>
public interface IPasswordResetService
{
    /// <summary>產生新的 token 並加進 repository，回傳要放進信裡的原文。</summary>
    string Issue(User user);

    /// <summary>寄出重設密碼信；寄不出去時丟例外。</summary>
    Task SendResetLinkAsync(User user, string rawToken, CancellationToken ct);

    /// <summary>寄出「密碼已變更」通知；寄不出去時丟例外。</summary>
    Task SendPasswordChangedAsync(User user, CancellationToken ct);
}

public sealed class PasswordResetService(
    IPasswordResetTokenRepository tokens,
    IEmailSender sender,
    AppOptions app,
    TimeProvider time) : IPasswordResetService
{
    public const string ResetSubject = "【Konbini】重設您的密碼";
    public const string ChangedSubject = "【Konbini】您的密碼已變更";

    public string Issue(User user)
    {
        var (token, raw) = PasswordResetToken.Issue(user.Id, time.GetUtcNow().UtcDateTime);
        tokens.Add(token);
        return raw;
    }

    public Task SendResetLinkAsync(User user, string rawToken, CancellationToken ct)
    {
        // token 是 base64url（A–Z、a–z、0–9、-、_），放進網址不需要再編碼
        var link = $"{app.ClientBaseUrl}/reset-password?token={rawToken}";
        var minutes = (int)PasswordResetToken.Lifetime.TotalMinutes;
        var name = WebUtility.HtmlEncode(user.Name);
        var href = WebUtility.HtmlEncode(link);

        var html = $"""
            <p>{name} 您好：</p>
            <p>我們收到重設 Konbini 密碼的申請。請在 {minutes} 分鐘內點下方按鈕設定新密碼，連結只能使用一次。</p>
            <p><a href="{href}" style="display:inline-block;padding:12px 24px;background:#d33a2c;color:#ffffff;text-decoration:none;border-radius:4px;font-weight:bold;">重設密碼</a></p>
            <p>按鈕無法點選時，請把下面的網址複製到瀏覽器開啟：<br>{href}</p>
            <p>如果不是您本人申請，請忽略這封信，您的密碼不會改變。</p>
            """;
        var text = $"""
            {user.Name} 您好：

            我們收到重設 Konbini 密碼的申請。請在 {minutes} 分鐘內開啟下面的網址設定新密碼，連結只能使用一次。

            {link}

            如果不是您本人申請，請忽略這封信，您的密碼不會改變。
            """;

        return sender.SendAsync(new EmailMessage(user.Email, ResetSubject, html, text), ct);
    }

    public Task SendPasswordChangedAsync(User user, CancellationToken ct)
    {
        // 通知信不帶 token：被冒用時由本人重新申請，舊的重設連結在重設成功時已全部失效
        var link = $"{app.ClientBaseUrl}/forgot-password";
        var name = WebUtility.HtmlEncode(user.Name);
        var href = WebUtility.HtmlEncode(link);

        var html = $"""
            <p>{name} 您好：</p>
            <p>您的 Konbini 密碼剛剛透過「忘記密碼」重設完成，之後請用新密碼登入。</p>
            <p>如果不是您本人操作，請立即到 Konbini 網站的「忘記密碼」重新設定密碼：<br>{href}</p>
            """;
        var text = $"""
            {user.Name} 您好：

            您的 Konbini 密碼剛剛透過「忘記密碼」重設完成，之後請用新密碼登入。

            如果不是您本人操作，請立即到 Konbini 網站的「忘記密碼」重新設定密碼：
            {link}
            """;

        return sender.SendAsync(new EmailMessage(user.Email, ChangedSubject, html, text), ct);
    }
}
