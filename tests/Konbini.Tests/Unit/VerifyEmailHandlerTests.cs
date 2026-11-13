using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Xunit;

namespace Konbini.Tests.Unit;

public class VerifyEmailHandlerTests
{
    private static readonly DateTime IssuedOn = new(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidToken_MarksUserVerified_AndTokenUsed()
    {
        var (token, raw) = IssueToken();
        var uow = new FakeUnitOfWork();
        var now = IssuedOn.AddHours(1);

        var result = await Handle(raw, token, uow, now);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.AlreadyVerified);
        Assert.Equal(now, token.User.EmailVerifiedOn);
        Assert.Equal(now, token.UsedOn);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task UsedToken_ReturnsAlreadyVerified_WithoutSaving()
    {
        var (token, raw) = IssueToken();
        token.UsedOn = IssuedOn.AddMinutes(5);
        token.User.EmailVerifiedOn = IssuedOn.AddMinutes(5);
        var uow = new FakeUnitOfWork();

        // 用過之後就算超過效期，再點一次也回「已完成驗證」
        var result = await Handle(raw, token, uow, IssuedOn + EmailVerificationToken.Lifetime + TimeSpan.FromDays(1));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.AlreadyVerified);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task OneSecondBeforeExpiry_IsAccepted()
    {
        var (token, raw) = IssueToken();

        var result = await Handle(raw, token, new FakeUnitOfWork(), token.ExpiresOn.AddSeconds(-1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AtExpiry_ReturnsExpired_WithoutSaving()
    {
        var (token, raw) = IssueToken();
        var uow = new FakeUnitOfWork();

        var result = await Handle(raw, token, uow, token.ExpiresOn);

        Assert.Same(AuthErrors.VerificationTokenExpired, result.Error);
        Assert.Null(token.User.EmailVerifiedOn);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task UnknownToken_ReturnsInvalid()
    {
        var (token, _) = IssueToken();

        var result = await Handle("not-a-real-token", token, new FakeUnitOfWork(), IssuedOn);

        Assert.Same(AuthErrors.VerificationTokenInvalid, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankToken_ReturnsTokenError(string raw)
    {
        var (token, _) = IssueToken();

        var result = await Handle(raw, token, new FakeUnitOfWork(), IssuedOn);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("token", result.Error!.Errors.Keys);
    }

    private static (EmailVerificationToken Token, string Raw) IssueToken()
    {
        var user = new User { Id = 1, Name = "王小明", Email = "user@example.com" };
        var (token, raw) = EmailVerificationToken.Issue(user, IssuedOn);
        token.User = user;
        return (token, raw);
    }

    private static Task<Result<VerifyEmailResponse>> Handle(
        string raw, EmailVerificationToken stored, FakeUnitOfWork uow, DateTime now) =>
        new VerifyEmailHandler(new FakeTokenRepository(stored), uow, new FixedTimeProvider(now))
            .Handle(new VerifyEmailCommand(new VerifyEmailRequest(raw)), CancellationToken.None);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>資料庫裡只有一筆 token。</summary>
    private sealed class FakeTokenRepository(EmailVerificationToken stored) : IEmailVerificationTokenRepository
    {
        public Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            Task.FromResult(tokenHash == stored.TokenHash ? stored : null);

        public Task<List<EmailVerificationToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Add(EmailVerificationToken token) => throw new NotSupportedException();
        public void Remove(EmailVerificationToken token) => throw new NotSupportedException();
    }
}
