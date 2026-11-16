using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// 重設密碼信中的 token。資料庫只存雜湊值、只能用一次（CLAUDE.md §6）；
/// 規則與理由見 docs/modules/decisions/Auth.md DD-AUT-002。
/// 不和 <see cref="EmailVerificationToken"/> 共用資料表：驗證連結用過再點回成功，重設連結用過要回錯誤。
/// </summary>
public class PasswordResetToken : AuditableEntity
{
    /// <summary>連結的效期：能直接接管帳號，比驗證連結（24 小時）短得多。</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    /// <summary>同一帳號兩次寄信至少間隔多久；以最新一筆 token 的 CreatedOn 判斷，和驗證信分開計算。</summary>
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);

    /// <summary>token_hash 欄位的長度。</summary>
    public const int HashLength = SecureToken.HashLength;

    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresOn { get; set; }

    /// <summary>完成重設的時間；null 表示還沒用過。用過的 token 不刪，再點一次時才能回「已使用過」。</summary>
    public DateTime? UsedOn { get; set; }

    /// <summary>產生新的 token。回傳的 RawToken 只用來組信中的連結，不寫進資料庫。</summary>
    public static (PasswordResetToken Token, string RawToken) Issue(int userId, DateTime now)
    {
        var (raw, hash) = SecureToken.Generate();
        return (new PasswordResetToken { UserId = userId, TokenHash = hash, ExpiresOn = now + Lifetime }, raw);
    }

    public static string Hash(string rawToken) => SecureToken.Hash(rawToken);

    public bool IsExpired(DateTime now) => now >= ExpiresOn;
}
