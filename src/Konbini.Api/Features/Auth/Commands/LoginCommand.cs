using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;

namespace Konbini.Api.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request);

public class LoginHandler(IUserRepository users, IPasswordHasher hasher, IJwtTokenService tokenService)
    : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return LoginResult.Fail(new() { ["general"] = "請輸入帳號與密碼" });
        }

        var user = await users.GetByEmailAsync(request.Email, ct);

        // 帳號不存在與密碼錯誤回同一訊息，避免帳號列舉
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return LoginResult.Fail(new() { ["general"] = "帳號或密碼錯誤" });
        }

        var token = tokenService.CreateToken(user);
        var dto = new UserDto(user.Id, user.Name, user.Email, user.Phone);
        return LoginResult.Ok(new LoginResponse(token, dto));
    }
}
