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
