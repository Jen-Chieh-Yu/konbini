using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record ResetPasswordCommand(ResetPasswordRequest Request);

/// <summary>
/// 點重設密碼信中的連結後設定新密碼：前端頁面讀出網址上的 token，和新密碼一起送過來（信中的連結本身不改狀態）。
/// 先檢查 token，token 有效才檢查密碼欄位：連結失效時，密碼打得再對也沒有用。
/// 不檢查新密碼是否和舊密碼相同：重設是為了拿回帳號，不是為了換密碼（DD-AUT-002）。
/// </summary>
public class ResetPasswordHandler(
    IPasswordResetTokenRepository tokens,
    IPasswordHasher hasher,
    IPasswordResetService passwordReset,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<ResetPasswordHandler> logger)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var request = command.Request;
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return Error.Validation("token", "重設連結不完整，請從信中重新點選連結");
        }

        var token = await tokens.GetByHashAsync(PasswordResetToken.Hash(request.Token.Trim()), ct);
        if (token is null)
        {
            return AuthErrors.ResetTokenInvalid;
        }
        if (token.UsedOn is not null)
        {
            return AuthErrors.ResetTokenUsed;
        }
        var now = time.GetUtcNow().UtcDateTime;
        if (token.IsExpired(now))
        {
            return AuthErrors.ResetTokenExpired;
        }

        var errors = new Dictionary<string, string>();
        if (PasswordPolicy.Validate(request.NewPassword) is { } passwordError)
        {
            errors["newPassword"] = passwordError;
        }
        if (request.NewPassword != request.ConfirmPassword)
        {
            errors["confirmPassword"] = "密碼與確認密碼不一致，請重新輸入";
        }
        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        var user = token.User;
        user.PasswordHash = hasher.Hash(request.NewPassword);
        // 點得到重設連結就證明信箱屬於本人，還沒驗證的帳號一併標為已驗證，重設完就能登入
        user.EmailVerifiedOn ??= now;
        token.UsedOn = now;
        // 同一帳號其他還沒用的重設連結一起失效：密碼已經換過，舊信裡的連結不該還能再改一次
        foreach (var other in await tokens.GetUnusedByUserAsync(user.Id, ct))
        {
            if (other.Id != token.Id)
            {
                tokens.Remove(other);
            }
        }
        await unitOfWork.SaveChangesAsync(ct);

        // 密碼已經改好，通知信寄不出去也回成功，只記 log（同註冊後寄驗證信）
        try
        {
            await passwordReset.SendPasswordChangedAsync(user, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "重設密碼後寄送通知信失敗，UserId {UserId}", user.Id);
        }

        return Result.Success();
    }
}
