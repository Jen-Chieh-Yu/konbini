using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// 寄到信箱的一次性 token：Email 驗證與重設密碼共用（CLAUDE.md §2 的抽象門檻：2 個使用者）。
/// 原文只出現在信裡，資料庫只存雜湊值。
/// </summary>
public static class SecureToken
{
    /// <summary>SHA-256 的十六進位字串長度，也是 token_hash 欄位的長度。</summary>
    public const int HashLength = 64;

    private const int TokenBytes = 32;

    /// <summary>產生 256 位元的亂數，回傳 base64url 原文（43 個字元，放進網址不需要再編碼）與雜湊值。</summary>
    public static (string Raw, string Hash) Generate()
    {
        var raw = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));
        return (raw, Hash(raw));
    }

    /// <summary>
    /// 用 SHA-256 而不是 PBKDF2：慢雜湊是為了保護低熵的密碼，256 位元的亂數本來就猜不到。
    /// </summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
