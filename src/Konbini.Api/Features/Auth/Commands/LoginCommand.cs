using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;

namespace Konbini.Api.Features.Auth.Commands;

public record LoginCommand(LoginRequest Request);

public class LoginHandler(IUserRepository users, IPasswordHasher hasher, IJwtTokenService tokenService)
    : ICommandHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var request = command.Request;

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthErrors.CredentialsRequired;
        }

        var user = await users.GetByEmailAsync(User.NormalizeEmail(request.Email), ct);

        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
        {
            return AuthErrors.InvalidCredentials;
        }

        var token = tokenService.CreateToken(user);
        var dto = new UserDto(user.Id, user.Name, user.Email, user.Phone);
        return new LoginResponse(token, dto);
    }
}
