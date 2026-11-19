using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Auth.Services;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Konbini.Tests.Unit;

public class ResetPasswordHandlerTests
{
    private const string NewPassword = "NewPass!23";
    private static readonly DateTime IssuedOn = new(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ValidToken_ChangesPassword_MarksTokenUsed_AndNotifies()
    {
        var env = new TestEnv();
        var now = IssuedOn.AddMinutes(10);

        var result = await env.Handle(env.Raw, NewPassword, NewPassword, now);

        Assert.True(result.IsSuccess);
        Assert.Equal("hashed:" + NewPassword, env.User.PasswordHash);
        Assert.Equal(now, env.Token.UsedOn);
        Assert.Equal(1, env.Uow.SaveCount);
        Assert.Equal(1, env.Reset.ChangedSaveCount); // 通知信在提交之後才寄
    }

    [Fact]
    public async Task UnverifiedUser_IsMarkedVerified()
    {
        var env = new TestEnv(verified: false);
        var now = IssuedOn.AddMinutes(10);

        await env.Handle(env.Raw, NewPassword, NewPassword, now);

        Assert.Equal(now, env.User.EmailVerifiedOn);
    }

    [Fact]
    public async Task VerifiedUser_KeepsOriginalVerifiedOn()
    {
        var env = new TestEnv(verified: true);
        var original = env.User.EmailVerifiedOn;

        await env.Handle(env.Raw, NewPassword, NewPassword, IssuedOn.AddMinutes(10));

        Assert.Equal(original, env.User.EmailVerifiedOn);
    }

    [Fact]
    public async Task Success_RemovesOtherUnusedTokens_ButNotTheUsedOne()
    {
        var env = new TestEnv();
        var other = new PasswordResetToken { Id = 99, UserId = env.User.Id, TokenHash = "other" };
        env.Tokens.Unused.Add(other);

        await env.Handle(env.Raw, NewPassword, NewPassword, IssuedOn.AddMinutes(10));

        Assert.Equal([other], env.Tokens.Removed);
    }

    [Fact]
    public async Task NotificationFails_StillSucceeds()
    {
        var env = new TestEnv();
        env.Reset.ThrowOnSend = true;

        // 密碼已經改好，通知信寄不出去只記 log
        var result = await env.Handle(env.Raw, NewPassword, NewPassword, IssuedOn.AddMinutes(10));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, env.Uow.SaveCount);
    }

    [Fact]
    public async Task OneSecondBeforeExpiry_IsAccepted()
    {
        var env = new TestEnv();

        var result = await env.Handle(env.Raw, NewPassword, NewPassword, env.Token.ExpiresOn.AddSeconds(-1));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AtExpiry_ReturnsExpired_WithoutSaving()
    {
        var env = new TestEnv();
        var originalHash = env.User.PasswordHash;

        var result = await env.Handle(env.Raw, NewPassword, NewPassword, env.Token.ExpiresOn);

        Assert.Same(AuthErrors.ResetTokenExpired, result.Error);
        Assert.Equal(originalHash, env.User.PasswordHash);
        Assert.Equal(0, env.Uow.SaveCount);
    }

    [Fact]
    public async Task UsedToken_ReturnsUsed_EvenIfPasswordIsInvalid()
    {
        var env = new TestEnv();
        env.Token.UsedOn = IssuedOn.AddMinutes(1);

        // 先檢查 token：連結已經失效，就不必再提示密碼哪裡不對
        var result = await env.Handle(env.Raw, "short", "different", IssuedOn.AddMinutes(10));

        Assert.Same(AuthErrors.ResetTokenUsed, result.Error);
        Assert.Equal(0, env.Uow.SaveCount);
    }

    [Fact]
    public async Task UnknownToken_ReturnsInvalid()
    {
        var env = new TestEnv();

        var result = await env.Handle("not-a-real-token", NewPassword, NewPassword, IssuedOn);

        Assert.Same(AuthErrors.ResetTokenInvalid, result.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankToken_ReturnsIncomplete(string raw)
    {
        var env = new TestEnv();

        var result = await env.Handle(raw, NewPassword, NewPassword, IssuedOn);

        Assert.Equal("重設連結不完整，請從信中重新點選連結", result.Error?.Errors["token"]);
    }

    [Fact]
    public async Task InvalidPasswordAndMismatch_ReturnsBothErrors()
    {
        var env = new TestEnv();

        var result = await env.Handle(env.Raw, "password", "Password", IssuedOn.AddMinutes(1));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("密碼需包含大寫英文字母、小寫英文字母、數字與符號", result.Error!.Errors["newPassword"]);
        Assert.Equal("密碼與確認密碼不一致，請重新輸入", result.Error.Errors["confirmPassword"]);
        Assert.Null(env.Token.UsedOn);
        Assert.Equal(0, env.Uow.SaveCount);
    }

    [Fact]
    public async Task SameAsOldPassword_IsAllowed()
    {
        var env = new TestEnv();
        env.User.PasswordHash = "hashed:" + NewPassword;

        // 重設是為了拿回帳號，不擋「和舊密碼相同」（改密碼才擋）
        var result = await env.Handle(env.Raw, NewPassword, NewPassword, IssuedOn.AddMinutes(1));

        Assert.True(result.IsSuccess);
    }

    /// <summary>資料庫裡只有一個帳號和一筆它的重設 token。</summary>
    private sealed class TestEnv
    {
        public TestEnv(bool verified = true)
        {
            User = new User
            {
                Id = 1,
                Name = "王小明",
                Email = "user@example.com",
                PasswordHash = "hashed:OldPass!23",
                EmailVerifiedOn = verified ? IssuedOn.AddDays(-30) : null,
            };
            (Token, Raw) = PasswordResetToken.Issue(User.Id, IssuedOn);
            Token.Id = 1;
            Token.User = User;
            Tokens = new FakeTokenRepository(Token);
            Reset = new FakePasswordResetService(Uow);
        }

        public User User { get; }
        public PasswordResetToken Token { get; }
        public string Raw { get; }
        public FakeUnitOfWork Uow { get; } = new();
        public FakeTokenRepository Tokens { get; }
        public FakePasswordResetService Reset { get; }

        public Task<Result> Handle(string raw, string newPassword, string confirmPassword, DateTime now) =>
            new ResetPasswordHandler(Tokens, new FakePasswordHasher(), Reset, Uow, new FixedTimeProvider(now),
                    NullLogger<ResetPasswordHandler>.Instance)
                .Handle(new ResetPasswordCommand(new ResetPasswordRequest(raw, newPassword, confirmPassword)),
                    CancellationToken.None);
    }

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

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed:" + password;
        public bool Verify(string password, string hash) => hash == Hash(password);
    }

    private sealed class FakePasswordResetService(FakeUnitOfWork uow) : IPasswordResetService
    {
        public bool ThrowOnSend { get; set; }
        public int? ChangedSaveCount { get; private set; }

        public Task SendPasswordChangedAsync(User user, CancellationToken ct)
        {
            if (ThrowOnSend) throw new InvalidOperationException("SMTP 連不上");
            ChangedSaveCount = uow.SaveCount;
            return Task.CompletedTask;
        }

        public string Issue(User user) => throw new NotSupportedException();
        public Task SendResetLinkAsync(User user, string rawToken, CancellationToken ct) => throw new NotSupportedException();
    }

    /// <summary>GetUnusedByUserAsync 比照資料庫：回傳 UsedOn 還是 null 的那份資料，包含正在使用的這一筆。</summary>
    private sealed class FakeTokenRepository(PasswordResetToken stored) : IPasswordResetTokenRepository
    {
        public List<PasswordResetToken> Unused { get; } = [stored];
        public List<PasswordResetToken> Removed { get; } = [];

        public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
            Task.FromResult(tokenHash == stored.TokenHash ? stored : null);

        public Task<List<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken ct) =>
            Task.FromResult(Unused.Where(t => t.UserId == userId).ToList());

        public void Remove(PasswordResetToken token) => Removed.Add(token);
        public void Add(PasswordResetToken token) => throw new NotSupportedException();
    }
}
