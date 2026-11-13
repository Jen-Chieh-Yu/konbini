using MimeKit;

namespace Konbini.Api.Features.Common.Email;

/// <summary>
/// 寄信設定（<c>Email:*</c>）。啟動時檢查，缺漏或格式錯誤就啟動失敗，
/// 不要等到第一個人註冊才發現寄不出去（比照 Jwt:Secret）。鍵一覽見 docs/configuration.md。
/// </summary>
public sealed class EmailOptions
{
    /// <summary>寄件者，例：<c>Konbini &lt;no-reply@konbini.test&gt;</c>。</summary>
    public required MailboxAddress From { get; init; }

    public required string SmtpHost { get; init; }

    public required int SmtpPort { get; init; }

    public static EmailOptions FromConfiguration(IConfiguration config)
    {
        var problems = new List<string>();

        var from = config["Email:From"];
        MailboxAddress? fromAddress = null;
        if (string.IsNullOrWhiteSpace(from))
        {
            problems.Add("Email:From 未設定");
        }
        else if (!MailboxAddress.TryParse(from, out fromAddress))
        {
            problems.Add("Email:From 不是合法的 Email 地址");
        }

        var host = config["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            problems.Add("Email:Smtp:Host 未設定");
        }

        var portText = config["Email:Smtp:Port"];
        if (!int.TryParse(portText, out var port) || port is < 1 or > 65535)
        {
            problems.Add("Email:Smtp:Port 必須是 1–65535 的整數");
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"寄信設定有誤：{string.Join("；", problems)}。預設值在 appsettings.json，容器的值在 compose 的 environment（docs/configuration.md）。");
        }

        return new EmailOptions { From = fromAddress!, SmtpHost = host!, SmtpPort = port };
    }
}
