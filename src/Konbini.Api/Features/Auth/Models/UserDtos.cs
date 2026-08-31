namespace Konbini.Api.Features.Auth.Models;

public record RegisterRequest(
    string LastName,
    string FirstName,
    string Email,
    string Password,
    string ConfirmPassword,
    string PhoneNumber,
    int Year,
    int Month,
    int Day);

public record LoginRequest(string Email, string Password);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record UserDto(int Id, string Name, string Email, string? Phone);

public record LoginResponse(string Token, UserDto User);

/// <summary>預期內失敗以錯誤字典回傳（key = 欄位名），不走例外。</summary>
public record AuthResult(bool Success, Dictionary<string, string> Errors)
{
    public static AuthResult Ok() => new(true, []);
    public static AuthResult Fail(Dictionary<string, string> errors) => new(false, errors);
}

public record LoginResult(bool Success, Dictionary<string, string> Errors, LoginResponse? Data)
{
    public static LoginResult Ok(LoginResponse data) => new(true, [], data);
    public static LoginResult Fail(Dictionary<string, string> errors) => new(false, errors, null);
}
