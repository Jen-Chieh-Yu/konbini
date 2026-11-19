using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Captcha.Errors;

/// <summary>
/// 驗證碼的錯誤，key 一律是 <c>captcha</c>，前端標在驗證碼欄位下方。
/// 回 400 不回 401／403：登入頁把 403 當成「帳號未驗證」，401 是帳號或密碼錯誤（DD-AUT-003）。
/// </summary>
public static class CaptchaErrors
{
    public const string Field = "captcha";

    public static readonly Error Required = Error.Validation(Field, "請輸入驗證碼");

    /// <summary>答錯、過期、用過、找不到都是同一個訊息：使用者要做的事都一樣，換一張重新輸入。</summary>
    public static readonly Error Invalid = Error.Validation(Field, "驗證碼錯誤或已過期，請輸入新的驗證碼");
}
