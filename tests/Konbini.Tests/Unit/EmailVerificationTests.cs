using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Email;
using Konbini.Api.Features.Common.Web;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Konbini.Tests.Unit;

/// <summary>token 的產生、驗證信的內容，以及寄信設定的啟動檢查。</summary>
public class EmailVerificationTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Issue_StoresHashOnly_AndExpiresAfterLifetime()
    {
        var (token, raw) = EmailVerificationToken.Issue(new User { Id = 5 }, Now);

        Assert.Equal(43, raw.Length); // 32 bytes 的 base64url，不含 =
        Assert.NotEqual(raw, token.TokenHash);
        Assert.Equal(EmailVerificationToken.Hash(raw), token.TokenHash);
        Assert.Equal(EmailVerificationToken.HashLength, token.TokenHash.Length);
        Assert.Equal(Now + EmailVerificationToken.Lifetime, token.ExpiresOn);
        Assert.Equal(5, token.UserId);
    }

    [Fact]
    public void Issue_ForNewUser_UsesNavigation()
    {
        var user = new User();

        var (token, _) = EmailVerificationToken.Issue(user, Now);

        // 還沒有 Id，由 EF 在同一次 SaveChanges 先寫入使用者
        Assert.Same(user, token.User);
    }

    [Fact]
    public void Issue_ReturnsDifferentTokenEachTime()
    {
        var user = new User { Id = 1 };

        Assert.NotEqual(EmailVerificationToken.Issue(user, Now).RawToken, EmailVerificationToken.Issue(user, Now).RawToken);
    }

    [Fact]
    public async Task SendAsync_LinkPointsToClient_AndNameIsHtmlEncoded()
    {
        var sender = new FakeEmailSender();
        var service = new EmailVerificationService(
            new NullTokenRepository(), sender, new AppOptions { ClientBaseUrl = "http://localhost:5173" }, TimeProvider.System);

        await service.SendAsync(new User { Name = "<b>王</b>", Email = "user@example.com" }, "abc_-123", CancellationToken.None);

        var mail = Assert.Single(sender.Sent);
        Assert.Equal("user@example.com", mail.To);
        Assert.Equal(EmailVerificationService.Subject, mail.Subject);
        Assert.Contains("http://localhost:5173/verify-email?token=abc_-123", mail.TextBody);
        Assert.Contains("http://localhost:5173/verify-email?token=abc_-123", mail.HtmlBody);
        Assert.Contains("&lt;b&gt;王&lt;/b&gt;", mail.HtmlBody);
        Assert.Contains("24 小時", mail.TextBody);
    }

    [Fact]
    public void EmailOptions_ValidConfiguration_IsParsed()
    {
        var options = EmailOptions.FromConfiguration(Config(new()
        {
            ["Email:From"] = "Konbini <no-reply@konbini.test>",
            ["Email:Smtp:Host"] = "mailpit",
            ["Email:Smtp:Port"] = "1025",
        }));

        Assert.Equal("no-reply@konbini.test", options.From.Address);
        Assert.Equal("mailpit", options.SmtpHost);
        Assert.Equal(1025, options.SmtpPort);
    }

    [Fact]
    public void EmailOptions_Missing_ListsEveryKey()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EmailOptions.FromConfiguration(Config(new())));

        Assert.Contains("Email:From", ex.Message);
        Assert.Contains("Email:Smtp:Host", ex.Message);
        Assert.Contains("Email:Smtp:Port", ex.Message);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("65536")]
    public void EmailOptions_InvalidPort_Throws(string port)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => EmailOptions.FromConfiguration(Config(new()
        {
            ["Email:From"] = "no-reply@konbini.test",
            ["Email:Smtp:Host"] = "mailpit",
            ["Email:Smtp:Port"] = port,
        })));

        Assert.Contains("Email:Smtp:Port", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("localhost:5173")]
    [InlineData("ftp://localhost")]
    public void AppOptions_InvalidClientBaseUrl_Throws(string? url)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AppOptions.FromConfiguration(Config(new() { ["App:ClientBaseUrl"] = url })));

        Assert.Contains("App:ClientBaseUrl", ex.Message);
    }

    [Fact]
    public void AppOptions_TrailingSlash_IsTrimmed()
    {
        var options = AppOptions.FromConfiguration(Config(new() { ["App:ClientBaseUrl"] = "http://localhost:8080/" }));

        Assert.Equal("http://localhost:8080", options.ClientBaseUrl);
    }

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken ct)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class NullTokenRepository : IEmailVerificationTokenRepository
    {
        public Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<List<EmailVerificationToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Add(EmailVerificationToken token) => throw new NotSupportedException();
        public void Remove(EmailVerificationToken token) => throw new NotSupportedException();
    }
}
