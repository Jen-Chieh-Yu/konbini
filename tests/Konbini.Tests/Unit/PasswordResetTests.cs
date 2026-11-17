using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Email;
using Konbini.Api.Features.Common.Web;
using Xunit;

namespace Konbini.Tests.Unit;

/// <summary>重設 token 的產生，以及重設信、通知信的內容。</summary>
public class PasswordResetTests
{
    private static readonly DateTime Now = new(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_StoresHashOnly_AndExpiresAfter30Minutes()
    {
        var (token, raw) = PasswordResetToken.Issue(5, Now);

        Assert.Equal(43, raw.Length); // 32 bytes 的 base64url，不含 =
        Assert.NotEqual(raw, token.TokenHash);
        Assert.Equal(PasswordResetToken.Hash(raw), token.TokenHash);
        Assert.Equal(PasswordResetToken.HashLength, token.TokenHash.Length);
        Assert.Equal(Now.AddMinutes(30), token.ExpiresOn);
        Assert.Equal(5, token.UserId);
    }

    [Fact]
    public void Issue_ReturnsDifferentTokenEachTime()
    {
        Assert.NotEqual(PasswordResetToken.Issue(1, Now).RawToken, PasswordResetToken.Issue(1, Now).RawToken);
    }

    [Fact]
    public async Task SendResetLinkAsync_LinkPointsToClient_AndNameIsHtmlEncoded()
    {
        var (service, sender) = Create();

        await service.SendResetLinkAsync(new User { Name = "<b>王</b>", Email = "user@example.com" }, "abc_-123", CancellationToken.None);

        var mail = Assert.Single(sender.Sent);
        Assert.Equal("user@example.com", mail.To);
        Assert.Equal(PasswordResetService.ResetSubject, mail.Subject);
        Assert.Contains("http://localhost:5173/reset-password?token=abc_-123", mail.TextBody);
        Assert.Contains("http://localhost:5173/reset-password?token=abc_-123", mail.HtmlBody);
        Assert.Contains("&lt;b&gt;王&lt;/b&gt;", mail.HtmlBody);
        Assert.Contains("30 分鐘", mail.TextBody);
    }

    [Fact]
    public async Task SendPasswordChangedAsync_HasNoToken_AndPointsToForgotPassword()
    {
        var (service, sender) = Create();

        await service.SendPasswordChangedAsync(new User { Name = "王小明", Email = "user@example.com" }, CancellationToken.None);

        var mail = Assert.Single(sender.Sent);
        Assert.Equal(PasswordResetService.ChangedSubject, mail.Subject);
        Assert.Contains("http://localhost:5173/forgot-password", mail.TextBody);
        Assert.DoesNotContain("token=", mail.HtmlBody);
    }

    private static (PasswordResetService, FakeEmailSender) Create()
    {
        var sender = new FakeEmailSender();
        var service = new PasswordResetService(
            new NullTokenRepository(), sender, new AppOptions { ClientBaseUrl = "http://localhost:5173" }, TimeProvider.System);
        return (service, sender);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken ct)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class NullTokenRepository : IPasswordResetTokenRepository
    {
        public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<List<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Add(PasswordResetToken token) => throw new NotSupportedException();
        public void Remove(PasswordResetToken token) => throw new NotSupportedException();
    }
}
