using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Konbini.Api.Features.Common.Email;

public sealed class MailKitEmailSender(EmailOptions options) : IEmailSender
{
    /// <summary>
    /// 連線加寄出的上限。寄信是同步的（註冊的請求會等它），SMTP 沒回應時不要讓請求卡住
    /// MailKit 預設的 2 分鐘。
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var mime = new MimeMessage();
        mime.From.Add(options.From);
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        using var client = new SmtpClient();
        // Mailpit 不加密、不需要帳密。換成真的 SMTP 服務時要改成 TLS 並加上帳密（CLAUDE.md §4 的上線前檢查項目）
        await client.ConnectAsync(options.SmtpHost, options.SmtpPort, SecureSocketOptions.None, timeout.Token);
        await client.SendAsync(mime, timeout.Token);
        await client.DisconnectAsync(quit: true, timeout.Token);
    }
}
