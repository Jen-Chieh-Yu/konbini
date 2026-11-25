namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// 註冊、重寄驗證信與忘記密碼共用的 Email 檢查，規則與訊息放在一起（同 <see cref="PasswordPolicy"/>）。
/// 傳入 <see cref="User.NormalizeEmail"/> 處理過的值：Handler 之後比對與儲存的也是它。
/// </summary>
public static class EmailPolicy
{
    public const string RequiredMessage = "請輸入 Email";
    public const string InvalidFormatMessage = "Email 格式不正確";

    /// <summary>上限是參數，C# 的 const 字串不能插入整數，所以用 static readonly。</summary>
    public static readonly string TooLongMessage = $"Email 不可超過 {User.EmailMaxLength} 個字元";

    /// <summary>依序檢查必填、長度、格式，只回第一條不符合的。</summary>
    /// <returns>不符合時回傳錯誤訊息，符合時回傳 null。</returns>
    public static string? Validate(string normalizedEmail)
    {
        if (normalizedEmail.Length == 0)
        {
            return RequiredMessage;
        }
        if (normalizedEmail.Length > User.EmailMaxLength)
        {
            return TooLongMessage;
        }
        if (!User.IsValidEmailFormat(normalizedEmail))
        {
            return InvalidFormatMessage;
        }
        return null;
    }
}
