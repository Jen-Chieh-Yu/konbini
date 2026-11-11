using Konbini.Api.Features.Auth.Models;
using Xunit;

namespace Konbini.Tests.Unit;

public class PasswordPolicyTests
{
    /// <summary>四種字元都有、指定長度的密碼。</summary>
    private static string Valid(int length) => "Aa1!" + new string('a', length - 4);

    [Theory]
    [InlineData(PasswordPolicy.MinLength)]
    [InlineData(PasswordPolicy.MaxLength)]
    public void LengthWithinRange_IsValid(int length)
    {
        Assert.Null(PasswordPolicy.Validate(Valid(length)));
    }

    [Theory]
    [InlineData(PasswordPolicy.MinLength - 1)]
    [InlineData(PasswordPolicy.MaxLength + 1)]
    public void LengthOutOfRange_ReturnsError(int length)
    {
        Assert.NotNull(PasswordPolicy.Validate(Valid(length)));
    }

    [Theory]
    [InlineData("password!1")]   // 沒有大寫
    [InlineData("PASSWORD!1")]   // 沒有小寫
    [InlineData("Password!!")]   // 沒有數字
    [InlineData("Password12")]   // 沒有符號
    [InlineData("Password1！")]  // 全形符號不算
    public void MissingCharacterKind_ReturnsError(string password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }

    [Theory]
    [InlineData("Pass word!1")]    // 中間
    [InlineData("Password!1 ")]    // 結尾
    [InlineData("Pass\tword!1")]   // Tab
    [InlineData("Pass　word!1")]   // 全形空白
    public void ContainsWhitespace_ReturnsError(string password)
    {
        Assert.Equal("密碼不可包含空白", PasswordPolicy.Validate(password));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("          ")]
    public void Blank_ReturnsError(string? password)
    {
        Assert.NotNull(PasswordPolicy.Validate(password));
    }
}
