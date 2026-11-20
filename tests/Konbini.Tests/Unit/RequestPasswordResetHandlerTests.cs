using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Xunit;

namespace Konbini.Tests.Unit;

public class RequestPasswordResetHandlerTests
{
    private const string VerifiedEmail = "verified@example.com";
    private const string UnverifiedEmail = "unverified@example.com";
    private static readonly DateTime Now = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(VerifiedEmail)]
    [InlineData(UnverifiedEmail)] // 還沒驗證也照寄：點得到連結就證明信箱屬於本人
    public async Task ExistingAccount_NoPendingToken_SendsAfterSaving(string email)
    {
        var (handler, reset, uow, _) = Create();

        var result = await Handle(handler, email);

        Assert.True(result.IsSuccess);
        Assert.Equal(email, reset.SentTo);
        Assert.Equal(1, reset.SaveCountAtSend);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task EmailIsTrimmedAndCaseInsensitive()
    {
        var (handler, reset, _, _) = Create();

        await Handle(handler, "  Verified@Example.COM ");

        Assert.Equal(VerifiedEmail, reset.SentTo);
    }

    [Fact]
    public async Task UnknownEmail_SucceedsWithoutSending()
    {
        var (handler, reset, uow, _) = Create();

        var result = await Handle(handler, "nobody@example.com");

        // 回應和真的寄出時一樣，看不出這個 Email 有沒有註冊
        Assert.True(result.IsSuccess);
        Assert.Null(reset.SentTo);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task PendingTokenOlderThanCooldown_RemovesItAndSends()
    {
        var old = PendingToken(sentAgo: PasswordResetToken.ResendCooldown);
        var (handler, reset, _, tokens) = Create(old);

        var result = await Handle(handler, VerifiedEmail);

        Assert.True(result.IsSuccess);
        Assert.Contains(old, tokens.Removed);
        Assert.NotNull(reset.SentTo);
    }

    [Fact]
    public async Task WithinCooldown_SucceedsWithoutSending()
    {
        var recent = PendingToken(sentAgo: PasswordResetToken.ResendCooldown - TimeSpan.FromSeconds(1));
        var (handler, reset, uow, tokens) = Create(recent);

        var result = await Handle(handler, VerifiedEmail);

        Assert.True(result.IsSuccess);
        Assert.Null(reset.SentTo);
        Assert.Empty(tokens.Removed);
        Assert.Equal(0, uow.SaveCount);
    }

    [Theory]
    [InlineData("", "請輸入 Email")]
    [InlineData("   ", "請輸入 Email")]
    [InlineData("not-an-email", "Email 格式不正確")]
    public async Task InvalidEmail_ReturnsEmailError(string email, string message)
    {
        var (handler, reset, _, _) = Create();

        var result = await Handle(handler, email);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal(message, result.Error!.Errors["email"]);
        Assert.Null(reset.SentTo);
    }

    [Fact]
    public async Task EmailTooLong_ReturnsFormatError()
    {
        var (handler, _, _, _) = Create();
        var email = new string('a', User.EmailMaxLength - "@example.com".Length + 1) + "@example.com";

        var result = await Handle(handler, email);

        Assert.Equal("Email 格式不正確", result.Error?.Errors["email"]);
    }

    [Fact]
    public async Task SendFails_Throws()
    {
        var (handler, reset, _, _) = Create();
        reset.ThrowOnSend = true;

        // 使用者主動要求寄信，寄不出去要讓他知道（交給 GlobalExceptionHandler 回 500）
        await Assert.ThrowsAsync<InvalidOperationException>(() => Handle(handler, VerifiedEmail));
    }

    private static PasswordResetToken PendingToken(TimeSpan sentAgo) =>
        new() { Id = 10, UserId = 1, TokenHash = "old", CreatedOn = Now - sentAgo, ExpiresOn = Now - sentAgo + PasswordResetToken.Lifetime };

    private static (RequestPasswordResetHandler, FakePasswordResetService, FakeUnitOfWork, FakeTokenRepository) Create(
        params PasswordResetToken[] pending)
    {
        var uow = new FakeUnitOfWork();
        var reset = new FakePasswordResetService(uow);
        var tokens = new FakeTokenRepository(pending);
        var handler = new RequestPasswordResetHandler(
            new FakeUserRepository(), tokens, reset, uow, new FixedTimeProvider(Now));
        return (handler, reset, uow, tokens);
    }

    private static Task<Result> Handle(RequestPasswordResetHandler handler, string email) =>
        handler.Handle(new RequestPasswordResetCommand(new RequestPasswordResetRequest(email)), CancellationToken.None);

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

        // 這個用例不開明確的交易
        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct) =>
            throw new NotSupportedException();
    }

    private sealed class FakePasswordResetService(FakeUnitOfWork uow) : IPasswordResetService
    {
        public bool ThrowOnSend { get; set; }
        public int? SaveCountAtSend { get; private set; }
        public string? SentTo { get; private set; }

        public string Issue(User user) => "raw-token";

        public Task SendResetLinkAsync(User user, string rawToken, CancellationToken ct)
        {
            if (ThrowOnSend) throw new InvalidOperationException("SMTP 連不上");
            SaveCountAtSend = uow.SaveCount;
            SentTo = user.Email;
            return Task.CompletedTask;
        }

        public Task SendPasswordChangedAsync(User user, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeTokenRepository(PasswordResetToken[] pending) : IPasswordResetTokenRepository
    {
        public List<PasswordResetToken> Removed { get; } = [];

        public Task<List<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            Task.FromResult(pending.Where(t => t.UserId == userId).ToList());

        public void Remove(PasswordResetToken token) => Removed.Add(token);

        public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Add(PasswordResetToken token) => throw new NotSupportedException();
    }

    /// <summary>只有一個已驗證（Id = 1）、一個未驗證（Id = 2）的帳號。</summary>
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
        public Task<UserRole?> GetRoleAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
