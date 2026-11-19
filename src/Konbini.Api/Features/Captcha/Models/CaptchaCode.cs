using System.Security.Cryptography;

namespace Konbini.Api.Features.Captcha.Models;

/// <summary>驗證碼的答案：5 個字元，排除容易看錯的 0 O 1 I L，比對時不分大小寫。</summary>
public static class CaptchaCode
{
    public const int Length = 5;

    /// <summary>圖中可能出現的字元。<see cref="CaptchaOptions.DevelopmentAnswer"/> 建議用不在這裡的字元，才不會和真的答案混在一起。</summary>
    public const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    /// <summary>用密碼學亂數產生：答案猜得到，驗證碼就沒有作用。</summary>
    public static string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);

    /// <summary>去掉前後空白、轉大寫後再比對。</summary>
    public static string Normalize(string answer) => answer.Trim().ToUpperInvariant();
}
