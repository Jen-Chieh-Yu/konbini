using System.Text.RegularExpressions;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Models;

public partial class User : AuditableEntity
{
    // 資料庫欄位長度的單一來源：UserConfiguration 與 Handler 的輸入驗證共用
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateOnly? Birthday { get; set; }
    public string? Phone { get; set; }

    /// <summary>完成 Email 驗證的時間；null 表示還沒驗證，不能登入。</summary>
    public DateTime? EmailVerifiedOn { get; set; }

    /// <summary>
    /// Email 以正規化後的小寫儲存與比對：主流信箱帳號都不分大小寫，
    /// 不正規化的話 Abc@x.com 與 abc@x.com 會變成兩個帳號（PostgreSQL 預設區分大小寫）。
    /// </summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Email 格式：名稱@網域.後綴，不可有空白、只能有一個 @。註冊與重寄驗證信共用。</summary>
    public static bool IsValidEmailFormat(string email) => EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
