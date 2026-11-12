using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Auth.Errors;

/// <summary>
/// Auth 的業務錯誤。欄位格式的檢查訊息（必填、長度、格式）寫在 Handler 的規則旁邊，不收在這裡
/// （docs/api-conventions.md「Handler 的結果表達」）。
/// </summary>
public static class AuthErrors
{
    public static readonly Error CredentialsRequired = Error.Validation(Error.GeneralKey, "請輸入帳號與密碼");

    /// <summary>帳號不存在與密碼錯誤共用同一個錯誤，避免從回應判斷哪些 Email 註冊過。</summary>
    public static readonly Error InvalidCredentials = Error.Unauthorized("帳號或密碼錯誤");

    /// <summary>token 有效但帳號已不存在。</summary>
    public static readonly Error UserNotFound = Error.NotFound("找不到使用者");

    /// <summary>回 400 不回 401：前端收到 401 會當成 token 失效而登出。</summary>
    public static readonly Error CurrentPasswordIncorrect = Error.Validation("currentPassword", "目前密碼不正確");

    /// <summary>
    /// 回 400 不回 409：要和其他欄位錯誤一起列出（CLAUDE.md §2「表單錯誤」）。
    /// 註冊時先查到重複，是放進欄位錯誤字典和其他欄位一起回傳，所以另外公開訊息本身。
    /// </summary>
    public const string EmailTakenMessage = "此Email已註冊過";

    public static readonly Error EmailTaken = Error.Validation("email", EmailTakenMessage);
}
