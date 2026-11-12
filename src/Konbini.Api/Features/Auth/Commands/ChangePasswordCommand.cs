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
            // token 有效但帳號已不存在
            return Error.NotFound("找不到使用者");
        }
        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            // 回 400 而不是 401：前端收到 401 會當成 token 失效而登出
            return Error.Validation("currentPassword", "目前密碼不正確");
        }
        if (PasswordPolicy.Validate(request.NewPassword) is { } passwordError)
        {
            return Error.Validation("newPassword", passwordError);
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
