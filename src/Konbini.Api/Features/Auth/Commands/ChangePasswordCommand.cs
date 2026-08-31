using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Auth.Models;

namespace Konbini.Api.Features.Auth.Commands;

public record ChangePasswordCommand(int UserId, ChangePasswordRequest Request);

public class ChangePasswordHandler(AppDbContext db, IPasswordHasher hasher)
    : ICommandHandler<ChangePasswordCommand, AuthResult>
{
    public async Task<AuthResult> Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var user = await db.Users.FindAsync([command.UserId], ct);

        if (user is null)
        {
            return AuthResult.Fail(new() { ["general"] = "找不到使用者" });
        }
        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return AuthResult.Fail(new() { ["currentPassword"] = "目前密碼不正確" });
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            return AuthResult.Fail(new() { ["newPassword"] = "密碼至少需要 8 個字元" });
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);
        return AuthResult.Ok();
    }
}
