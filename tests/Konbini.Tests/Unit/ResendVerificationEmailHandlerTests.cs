using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Xunit;

namespace Konbini.Tests.Unit;

public class ResendVerificationEmailHandlerTests
{
    private const string UnverifiedEmail = "unverified@example.com";
    private const string VerifiedEmail = "verified@example.com";
    private static readonly DateTime Now = new(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Unverified_NoPendingToken_Sends()
    {
        var (handler, verification, uow, _) = Create();

        var result = await Handle(handler, UnverifiedEmail);

        Assert.True(result.IsSuccess);
        Assert.Equal(UnverifiedEmail, verification.SentTo);
        Assert.Equal(1, verification.SaveCountAtSend);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task EmailIsTrimmedAndCaseInsensitive()
    {
        var (handler, verification, _, _) = Create();

        await Handle(handler, "  Unverified@Example.COM ");

        Assert.Equal(UnverifiedEmail, verification.SentTo);
    }

    [Fact]
    public async Task PendingTokenOlderThanCooldown_RemovesItAndSends()
    {
        var old = PendingToken(sentAgo: EmailVerificationToken.ResendCooldown);
        var (handler, verification, _, tokens) = Create(old);

        var result = await Handle(handler, UnverifiedEmail);

        Assert.True(result.IsSuccess);
        Assert.Contains(old, tokens.Removed);
        Assert.NotNull(verification.SentTo);
    }

    [Fact]
    public async Task WithinCooldown_SucceedsWithoutSending()
    {
        var recent = PendingToken(sentAgo: EmailVerificationToken.ResendCooldown - TimeSpan.FromSeconds(1));
        var (handler, verification, uow, tokens) = Create(recent);

        var result = await Handle(handler, UnverifiedEmail);

        Assert.True(result.IsSuccess);
        Assert.Null(verification.SentTo);
        Assert.Empty(tokens.Removed);
        Assert.Equal(0, uow.SaveCount);
    }

    [Theory]
    [InlineData(VerifiedEmail)]
    [InlineData("nobody@example.com")]
    public async Task VerifiedOrUnknown_SucceedsWithoutSending(string email)
    {
        var (handler, verification, uow, _) = Create();

        var result = await Handle(handler, email);

        // 回應和真的寄出時一樣，看不出這個 Email 有沒有註冊
        Assert.True(result.IsSuccess);
        Assert.Null(verification.SentTo);
        Assert.Equal(0, uow.SaveCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task InvalidEmail_ReturnsEmailError(string email)
    {
        var (handler, verification, _, _) = Create();

        var result = await Handle(handler, email);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("email", result.Error!.Errors.Keys);
        Assert.Null(verification.SentTo);
    }

    [Fact]
    public async Task SendFails_Throws()
    {
        var (handler, verification, _, _) = Create();
        verification.ThrowOnSend = true;

        // 使用者主動要求寄信，寄不出去要讓他知道（交給 GlobalExceptionHandler 回 500）
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(handler, UnverifiedEmail));
    }

    private static EmailVerificationToken PendingToken(TimeSpan sentAgo) =>
        new() { Id = 10, UserId = 2, TokenHash = "old", CreatedOn = Now - sentAgo, ExpiresOn = Now - sentAgo + EmailVerificationToken.Lifetime };

    private static (ResendVerificationEmailHandler, FakeVerificationService, FakeUnitOfWork, FakeTokenRepository) Create(
        params EmailVerificationToken[] pending)
    {
        var uow = new FakeUnitOfWork();
        var verification = new FakeVerificationService(uow);
        var tokens = new FakeTokenRepository(pending);
        var handler = new ResendVerificationEmailHandler(
            new FakeUserRepository(), tokens, verification, uow, new FixedTimeProvider(Now));
        return (handler, verification, uow, tokens);
    }

    private static Task<Result> Handle(ResendVerificationEmailHandler handler, string email) =>
        handler.Handle(new ResendVerificationEmailCommand(new ResendVerificationEmailRequest(email)), CancellationToken.None);

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

    private sealed class FakeVerificationService(FakeUnitOfWork uow) : IEmailVerificationService
    {
        public bool ThrowOnSend { get; set; }
        public int? SaveCountAtSend { get; private set; }
        public string? SentTo { get; private set; }

        public string Issue(User user) => "raw-token";

        public Task SendAsync(User user, string rawToken, CancellationToken ct)
        {
            if (ThrowOnSend) throw new InvalidOperationException("SMTP 連不上");
            SaveCountAtSend = uow.SaveCount;
            SentTo = user.Email;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTokenRepository(EmailVerificationToken[] pending) : IEmailVerificationTokenRepository
    {
        public List<EmailVerificationToken> Removed { get; } = [];

        public Task<List<EmailVerificationToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            Task.FromResult(pending.Where(t => t.UserId == userId).ToList());

        public void Remove(EmailVerificationToken token) => Removed.Add(token);

        public Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Add(EmailVerificationToken token) => throw new NotSupportedException();
    }

    /// <summary>只有一個已驗證、一個未驗證（Id = 2）的帳號。</summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => Task.FromResult(email switch
        {
            VerifiedEmail => new User { Id = 1, Name = "王小明", Email = VerifiedEmail, EmailVerifiedOn = Now.AddDays(-1) },
            UnverifiedEmail => new User { Id = 2, Name = "陳小華", Email = UnverifiedEmail },
            _ => null,
        });

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
