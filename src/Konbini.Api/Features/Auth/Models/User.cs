namespace Konbini.Api.Features.Auth.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateOnly? Birthday { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Email 以正規化後的小寫儲存與比對：主流信箱帳號都不分大小寫，
    /// 不正規化的話 Abc@x.com 與 abc@x.com 會變成兩個帳號（PostgreSQL 預設區分大小寫）。
    /// </summary>
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
