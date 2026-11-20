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

/// <param name="Role">前端用來顯示後台入口；後台的防護在後端（Admin policy），不靠畫面隱藏。</param>
public record UserDto(int Id, string Name, string Email, string? Phone, UserRole Role);

public record LoginResponse(string Token, UserDto User);

public record VerifyEmailRequest(string Token);

/// <summary>AlreadyVerified：這個連結之前就用過了（例：連點兩次），帳號早已完成驗證。</summary>
public record VerifyEmailResponse(bool AlreadyVerified);

public record ResendVerificationEmailRequest(string Email);

public record RequestPasswordResetRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);
