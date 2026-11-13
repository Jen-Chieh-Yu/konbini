using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record ResendVerificationEmailCommand(ResendVerificationEmailRequest Request);

/// <summary>
/// 重寄驗證信。不需要登入（未驗證的帳號登入不了）。
/// Email 不存在、已經驗證、還在冷卻時間內，都回成功但不寄信：回應一致，看不出哪些 Email 註冊過
/// （OWASP Authentication Cheat Sheet）；畫面上的冷卻倒數由前端負責。
/// </summary>
public class ResendVerificationEmailHandler(
    IUserRepository users,
    IEmailVerificationTokenRepository tokens,
    IEmailVerificationService verification,
    IUnitOfWork unitOfWork,
    TimeProvider time)
    : ICommandHandler<ResendVerificationEmailCommand, Result>
{
    public async Task<Result> Handle(ResendVerificationEmailCommand command, CancellationToken ct)
    {
        // 先去頭尾空白、轉小寫再檢查，與註冊一致（docs/api-conventions.md「驗證」）
        var email = command.Request.Email is null ? "" : User.NormalizeEmail(command.Request.Email);
        if (email.Length == 0)
        {
            return Error.Validation("email", "請輸入 Email");
        }
        if (email.Length > User.EmailMaxLength || !User.IsValidEmailFormat(email))
        {
            return Error.Validation("email", "Email 格式不正確");
        }

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || user.EmailVerifiedOn is not null)
        {
            return Result.Success();
        }

        var pending = await tokens.GetUnusedByUserAsync(user.Id, ct);
        var now = time.GetUtcNow().UtcDateTime;
        if (pending.Any(t => now - t.CreatedOn < EmailVerificationToken.ResendCooldown))
        {
            return Result.Success();
        }

        // 只有最新一封信的連結有效：舊的連結失效後點了會看到「驗證連結無效」
        foreach (var old in pending)
        {
            tokens.Remove(old);
        }
        var rawToken = verification.Issue(user);
        await unitOfWork.SaveChangesAsync(ct);

        // 寄不出去時讓例外往外丟（500）：使用者主動要求寄信，要讓他知道沒寄出去
        await verification.SendAsync(user, rawToken, ct);
        return Result.Success();
    }
}
