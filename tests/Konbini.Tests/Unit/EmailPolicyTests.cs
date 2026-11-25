using Konbini.Api.Features.Auth.Models;
using Xunit;

namespace Konbini.Tests.Unit;

public class EmailPolicyTests
{
    private static string OfLength(int length)
    {
        const string domain = "@example.com";
        return new string('a', length - domain.Length) + domain;
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("a@b.c")]   // 後端與前端都接受的最短形式
    public void ValidFormat_IsValid(string email)
    {
        Assert.Null(EmailPolicy.Validate(email));
    }

    [Fact]
    public void Empty_ReturnsRequired()
    {
        Assert.Equal(EmailPolicy.RequiredMessage, EmailPolicy.Validate(""));
    }

    [Fact]
    public void AtMaxLength_IsValid()
    {
        Assert.Null(EmailPolicy.Validate(OfLength(User.EmailMaxLength)));
    }

    [Fact]
    public void OverMaxLength_ReturnsTooLong()
    {
        Assert.Equal(EmailPolicy.TooLongMessage, EmailPolicy.Validate(OfLength(User.EmailMaxLength + 1)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]       // 沒有後綴
    [InlineData("a b@c.d")]   // 有空白
    [InlineData("a@@b.c")]    // 兩個 @
    public void InvalidFormat_ReturnsFormatError(string email)
    {
        Assert.Equal(EmailPolicy.InvalidFormatMessage, EmailPolicy.Validate(email));
    }
}
