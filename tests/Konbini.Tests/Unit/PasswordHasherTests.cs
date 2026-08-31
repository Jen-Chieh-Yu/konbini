using Konbini.Api.Features.Common.Auth;
using Xunit;

namespace Konbini.Tests.Unit;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotContainPlainPassword()
    {
        var hash = _hasher.Hash("MySecret123");
        Assert.DoesNotContain("MySecret123", hash);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("MySecret123");
        Assert.True(_hasher.Verify("MySecret123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("MySecret123");
        Assert.False(_hasher.Verify("WrongPassword", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        // 隨機 salt：同一組密碼兩次雜湊必須不同
        Assert.NotEqual(_hasher.Hash("MySecret123"), _hasher.Hash("MySecret123"));
    }

    [Fact]
    public void Verify_MalformedHash_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("MySecret123", "not-a-valid-hash"));
    }
}
