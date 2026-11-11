namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// 註冊與改密碼共用的密碼規則，比照常見線上服務。密碼存的是雜湊值，上限與資料庫欄位無關。
/// 登入不套用：先前註冊的帳號可能不符合現行規則。
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 20;

    /// <returns>不符合時回傳錯誤訊息，符合時回傳 null。</returns>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
        {
            return $"密碼至少需要 {MinLength} 個字元";
        }
        if (password.Length > MaxLength)
        {
            return $"密碼不可超過 {MaxLength} 個字元";
        }
        if (password.Any(char.IsWhiteSpace))
        {
            return "密碼不可包含空白";
        }
        if (!password.Any(char.IsAsciiLetterUpper) || !password.Any(char.IsAsciiLetterLower)
            || !password.Any(char.IsAsciiDigit) || !password.Any(IsSymbol))
        {
            return "密碼需包含大寫英文字母、小寫英文字母、數字與符號";
        }
        return null;
    }

    /// <summary>鍵盤上的半形符號（!@#$% 等）；空白、中文、全形符號不算。</summary>
    private static bool IsSymbol(char c) => c is > ' ' and <= '~' && !char.IsAsciiLetterOrDigit(c);
}
