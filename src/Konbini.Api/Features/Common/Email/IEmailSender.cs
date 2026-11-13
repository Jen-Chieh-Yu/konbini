namespace Konbini.Api.Features.Common.Email;

/// <summary>一封要寄出的信。HTML 與純文字各一份，收信軟體不顯示 HTML 時看純文字。</summary>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);

/// <summary>
/// 寄信。Email 驗證與之後的忘記密碼共用（CLAUDE.md §6）；開發與部署環境都寄到 Mailpit（§4）。
/// 寄不出去時丟例外，由呼叫端決定要不要讓請求失敗。
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}
