using System.Diagnostics.CodeAnalysis;

namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>可預期失敗的類型，Endpoint 依此對應 HTTP 狀態碼（<c>Common/Web/ErrorResults</c>）。</summary>
public enum ErrorType
{
    /// <summary>400：輸入不合法，包含 body 裡參照到不存在的資料（例：下單的商品 Id）。</summary>
    Validation,

    /// <summary>401：身分驗證失敗（例：登入的帳號或密碼錯誤）。</summary>
    Unauthorized,

    /// <summary>403：已確認身分，但不允許這個操作。</summary>
    Forbidden,

    /// <summary>404：網址指到的資源不存在，或不屬於目前使用者。</summary>
    NotFound,

    /// <summary>409：與目前的資料狀態衝突（例：重複下單、庫存不足）。</summary>
    Conflict,
}

/// <summary>
/// 可預期的失敗。Errors 的 key 對應輸入欄位（camelCase），不屬於特定欄位的用 <see cref="GeneralKey"/>；
/// 至少有一項，前端一律從 errors 讀訊息。
/// </summary>
public sealed class Error
{
    public const string GeneralKey = "general";

    private Error(ErrorType type, IReadOnlyDictionary<string, string> errors)
    {
        if (errors.Count == 0)
        {
            throw new ArgumentException("錯誤至少要有一項。", nameof(errors));
        }
        Type = type;
        Errors = errors;
    }

    public ErrorType Type { get; }

    public IReadOnlyDictionary<string, string> Errors { get; }

    /// <summary>多個欄位的錯誤，一次回傳。會複製一份，呼叫端之後再改字典不影響結果。</summary>
    public static Error Validation(IReadOnlyDictionary<string, string> errors) =>
        new(ErrorType.Validation, new Dictionary<string, string>(errors));

    public static Error Validation(string field, string message) => new(ErrorType.Validation, One(field, message));

    public static Error Unauthorized(string message) => new(ErrorType.Unauthorized, One(GeneralKey, message));

    public static Error Forbidden(string message) => new(ErrorType.Forbidden, One(GeneralKey, message));

    public static Error NotFound(string message) => new(ErrorType.NotFound, One(GeneralKey, message));

    public static Error Conflict(string field, string message) => new(ErrorType.Conflict, One(field, message));

    private static Dictionary<string, string> One(string key, string message) => new() { [key] = message };
}

/// <summary>
/// 用例的結果：成功，或一個可預期的 <see cref="Error"/>。非預期的狀況照樣丟例外，交給 GlobalExceptionHandler。
/// Handler 內可以直接 <c>return Error.Validation(...)</c>，由隱式轉換變成失敗的結果。
/// </summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>
/// 帶回傳值的結果。Handler 內可以直接 <c>return dto;</c> 或 <c>return Error.NotFound(...)</c>。
/// 查無資料要明寫成 NotFound，不提供「null 自動變失敗」的轉換。
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error)
    {
    }

    /// <summary>成功時的回傳值；失敗時存取會丟例外，先檢查 <see cref="Result.IsSuccess"/>。</summary>
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("失敗的結果沒有回傳值。");

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
