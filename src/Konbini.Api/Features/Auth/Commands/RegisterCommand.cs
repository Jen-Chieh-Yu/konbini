using System.Text.RegularExpressions;
using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;

namespace Konbini.Api.Features.Auth.Commands;

public record RegisterCommand(RegisterRequest Request);

public partial class RegisterHandler(IUserRepository users, IPasswordHasher hasher, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterCommand, Result>
{
    [GeneratedRegex(@"^09\d{8}$")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();

    public async Task<Result> Handle(RegisterCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string>();
        // 先去頭尾空白再驗證，長度與儲存都用處理後的值；Email 與登入一樣先正規化
        var lastName = request.LastName?.Trim() ?? "";
        var firstName = request.FirstName?.Trim() ?? "";
        var email = request.Email is null ? "" : User.NormalizeEmail(request.Email);
        var phone = request.PhoneNumber?.Trim() ?? "";

        if (lastName.Length == 0)
        {
            errors["lastName"] = "請輸入姓氏";
        }
        if (firstName.Length == 0)
        {
            errors["firstName"] = "請輸入名字";
        }
        // 姓與名合併存進 users.name，上限看的是兩者合計
        if (lastName.Length + firstName.Length > User.NameMaxLength)
        {
            errors["name"] = $"姓名合計不可超過 {User.NameMaxLength} 個字元";
        }

        if (email.Length == 0)
        {
            errors["email"] = "請填寫Email";
        }
        else if (email.Length > User.EmailMaxLength)
        {
            errors["email"] = $"Email不可超過 {User.EmailMaxLength} 個字元";
        }
        else if (!EmailRegex().IsMatch(email))
        {
            errors["email"] = "Email格式不正確";
        }
        else if (await users.EmailExistsAsync(email, ct))
        {
            errors["email"] = AuthErrors.EmailTakenMessage;
        }

        if (PasswordPolicy.Validate(request.Password) is { } passwordError)
        {
            errors["password"] = passwordError;
        }
        if (request.Password != request.ConfirmPassword)
        {
            errors["confirmPassword"] = "密碼與確認密碼不一致，請重新輸入";
        }

        if (phone.Length == 0)
        {
            errors["phoneNumber"] = "請填寫手機號碼";
        }
        else if (!PhoneRegex().IsMatch(phone))
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
            return Error.Validation(errors);
        }

        users.Add(new User
        {
            Name = lastName + firstName,
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            Birthday = birthday,
            Phone = phone,
        });

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            // 先查時還沒有人用，寫入前被同時送出的請求搶先；users 只有 email 一個唯一索引
            return AuthErrors.EmailTaken;
        }

        return Result.Success();
    }
}
