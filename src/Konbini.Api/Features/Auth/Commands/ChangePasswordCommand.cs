using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record ChangePasswordCommand(int UserId, ChangePasswordRequest Request);

public class ChangePasswordHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var user = await users.GetByIdAsync(command.UserId, ct);

        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            return Error.Validation("currentPassword", "請輸入目前密碼");
        }
        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return AuthErrors.CurrentPasswordIncorrect;
        }
        if (PasswordPolicy.Validate(request.NewPassword) is { } passwordError)
        {
            return Error.Validation("newPassword", passwordError);
        }
        if (request.NewPassword == request.CurrentPassword)
        {
            return Error.Validation("newPassword", "新密碼不可與目前密碼相同");
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
