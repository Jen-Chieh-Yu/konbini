using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record RequestPasswordResetCommand(RequestPasswordResetRequest Request);

/// <summary>
/// 忘記密碼：寄出重設密碼信。不需要登入。
/// Email 不存在、還在冷卻時間內，都回成功但不寄信：回應一致，看不出哪些 Email 註冊過
/// （OWASP Forgot Password Cheat Sheet）；畫面上的冷卻倒數由前端負責。
/// 還沒完成 Email 驗證的帳號照樣寄：點得到重設連結就證明信箱屬於本人（DD-AUT-002）。
/// </summary>
public class RequestPasswordResetHandler(
    IUserRepository users,
    IPasswordResetTokenRepository tokens,
    IPasswordResetService passwordReset,
    IUnitOfWork unitOfWork,
    TimeProvider time)
    : ICommandHandler<RequestPasswordResetCommand, Result>
{
    public async Task<Result> Handle(RequestPasswordResetCommand command, CancellationToken ct)
    {
        // 先去頭尾空白、轉小寫再檢查，與註冊一致（docs/api-conventions.md「驗證」）
        var email = command.Request.Email is null ? "" : User.NormalizeEmail(command.Request.Email);
        if (EmailPolicy.Validate(email) is { } emailError)
        {
            return Error.Validation("email", emailError);
        }

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null)
        {
            return Result.Success();
        }

        var pending = await tokens.GetUnusedByUserAsync(user.Id, ct);
        var now = time.GetUtcNow().UtcDateTime;
        if (pending.Any(t => now - t.CreatedOn < PasswordResetToken.ResendCooldown))
        {
            return Result.Success();
        }

        // 只有最新一封信的連結有效：舊的連結失效後點了會看到「重設連結無效」
        foreach (var old in pending)
        {
            tokens.Remove(old);
        }
        var rawToken = passwordReset.Issue(user);
        await unitOfWork.SaveChangesAsync(ct);

        // 寄不出去時讓例外往外丟（500）：使用者主動要求寄信，要讓他知道沒寄出去（同重寄驗證信）
        await passwordReset.SendResetLinkAsync(user, rawToken, ct);
        return Result.Success();
    }
}
