using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record VerifyEmailCommand(VerifyEmailRequest Request);

/// <summary>點驗證信中的連結：前端頁面讀出網址上的 token 後送過來（信中的連結本身不改狀態）。</summary>
public class VerifyEmailHandler(IEmailVerificationTokenRepository tokens, IUnitOfWork unitOfWork, TimeProvider time)
    : ICommandHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    public async Task<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand command, CancellationToken ct)
    {
        var raw = command.Request.Token;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Error.Validation("token", "驗證連結不完整，請從信中重新點選連結");
        }

        var token = await tokens.GetByHashAsync(EmailVerificationToken.Hash(raw.Trim()), ct);
        if (token is null)
        {
            return AuthErrors.VerificationTokenInvalid;
        }

        // 用過的連結再點一次不算錯：持有 token 已經證明信箱屬於本人，連點兩次的人不該看到錯誤畫面
        if (token.UsedOn is not null)
        {
            return new VerifyEmailResponse(AlreadyVerified: true);
        }

        var now = time.GetUtcNow().UtcDateTime;
        if (token.IsExpired(now))
        {
            return AuthErrors.VerificationTokenExpired;
        }

        var alreadyVerified = token.User.EmailVerifiedOn is not null;
        token.UsedOn = now;
        token.User.EmailVerifiedOn ??= now;
        await unitOfWork.SaveChangesAsync(ct);

        return new VerifyEmailResponse(alreadyVerified);
    }
}
