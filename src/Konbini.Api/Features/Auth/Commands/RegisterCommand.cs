using System.Text.RegularExpressions;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record RegisterCommand(RegisterRequest Request);

public partial class RegisterHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCommand, AuthResult>
{
    private const string EmailTakenMessage = "此Email已註冊過";

    [GeneratedRegex(@"^09\d{8}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public async Task<AuthResult> Handle(RegisterCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.LastName))
        {
            errors["lastName"] = "請輸入姓氏";
        }
        if (string.IsNullOrWhiteSpace(request.FirstName))
        {
            errors["firstName"] = "請輸入名字";
        }
        // 姓與名合併存進 users.name，上限看的是兩者合計
        if ((request.LastName?.Length ?? 0) + (request.FirstName?.Length ?? 0) > User.NameMaxLength)
        {
            errors["name"] = $"姓名合計不可超過 {User.NameMaxLength} 個字元";
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors["email"] = "請填寫Email";
        }
        else if (request.Email.Length > User.EmailMaxLength)
        {
            errors["email"] = $"Email不可超過 {User.EmailMaxLength} 個字元";
        }
        else if (!EmailRegex().IsMatch(request.Email))
        {
            errors["email"] = "Email格式不正確";
        }
        else if (await users.EmailExistsAsync(User.NormalizeEmail(request.Email), ct))
        {
            errors["email"] = EmailTakenMessage;
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            errors["password"] = "密碼至少需要 8 個字元";
        }
        if (request.Password != request.ConfirmPassword)
        {
            errors["confirmPassword"] = "密碼與確認密碼不一致，請重新輸入";
        }

        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            errors["phoneNumber"] = "請填寫手機號碼";
        }
        else if (!PhoneRegex().IsMatch(request.PhoneNumber))
        {
            errors["phoneNumber"] = "手機號碼格式不正確";
        }

        DateOnly? birthday = null;
        if (request.Year == 0 || request.Month == 0 || request.Day == 0)
        {
            errors["birthday"] = "請選擇完整的出生年月日";
        }
        else
        {
            try
            {
                birthday = new DateOnly(request.Year, request.Month, request.Day);
            }
            catch (ArgumentOutOfRangeException)
            {
                errors["birthday"] = "出生日期不正確";
            }
        }

        if (errors.Count > 0)
        {
            return AuthResult.Fail(errors);
        }

        users.Add(new User
        {
            Name = request.LastName + request.FirstName,
            Email = User.NormalizeEmail(request.Email),
            PasswordHash = hasher.Hash(request.Password),
            Birthday = birthday,
            Phone = request.PhoneNumber,
        });

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            // 先查時還沒有人用，寫入前被同時送出的請求搶先；users 只有 email 一個唯一索引
            return AuthResult.Fail(new() { ["email"] = EmailTakenMessage });
        }

        return AuthResult.Ok();
    }
}
