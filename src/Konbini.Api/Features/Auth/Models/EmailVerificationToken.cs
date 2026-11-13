using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// Email 驗證信中的 token。資料庫只存雜湊值，原文只出現在信裡（CLAUDE.md §6）；
/// 規則與理由見 docs/modules/decisions/Auth.md DD-AUT-001。
/// </summary>
public class EmailVerificationToken : AuditableEntity
{
    /// <summary>連結的效期。</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>同一帳號兩次寄信至少間隔多久；以最新一筆 token 的 CreatedOn 判斷。</summary>
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>SHA-256 的十六進位字串長度，也是 token_hash 欄位的長度。</summary>
    public const int HashLength = 64;

    private const int TokenBytes = 32;

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }

    /// <summary>完成驗證的時間；null 表示還沒用過。用過的 token 不刪，再點一次時才能回「已完成驗證」。</summary>
    public DateTime? UsedOn { get; set; }

    /// <summary>
    /// 產生新的 token。回傳的 RawToken 只用來組信中的連結，不寫進資料庫。
    /// 新註冊的帳號還沒有 Id，用導覽屬性讓 EF 在同一次 SaveChanges 先寫入使用者；
    /// 已存在的帳號只設 UserId，避免把未追蹤的 User 當成新資料再寫入一次。
    /// </summary>
    public static (EmailVerificationToken Token, string RawToken) Issue(User user, DateTime now)
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        var token = new EmailVerificationToken { TokenHash = Hash(raw), ExpiresOn = now + Lifetime };
        if (user.Id == 0)
        {
            token.User = user;
        }
        else
        {
            token.UserId = user.Id;
        }
        return (token, raw);
    }

    /// <summary>
    /// 用 SHA-256 而不是 PBKDF2：慢雜湊是為了保護低熵的密碼，256 位元的亂數本來就猜不到。
    /// </summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    public bool IsExpired(DateTime now) => now >= ExpiresOn;
}
