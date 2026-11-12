using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;
using Xunit;

namespace Konbini.Tests.Unit;

public class ChangePasswordHandlerTests
{
    /// <summary>四種字元都有、指定長度的密碼。</summary>
    private static string PasswordOfLength(int length) => "Aa1!" + new string('a', length - 4);

    [Fact]
    public async Task NewPasswordAtLimit_IsSaved()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(PasswordOfLength(PasswordPolicy.MaxLength), uow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task NewPasswordOverLimit_ReturnsNewPasswordError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(PasswordOfLength(PasswordPolicy.MaxLength + 1), uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("newPassword", result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task WrongCurrentPassword_ReturnsCurrentPasswordValidationError()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(PasswordOfLength(PasswordPolicy.MinLength), uow, currentPasswordMatches: false);

        // 不能是 Unauthorized：前端收到 401 會把使用者登出
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("currentPassword", result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task UserNoLongerExists_ReturnsNotFound()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(PasswordOfLength(PasswordPolicy.MinLength), uow, userExists: false);

        Assert.Equal(ErrorType.NotFound, result.Error?.Type);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<Result> Handle(
        string newPassword, FakeUnitOfWork uow, bool userExists = true, bool currentPasswordMatches = true) =>
        new ChangePasswordHandler(new FakeUserRepository(userExists), new FakePasswordHasher(currentPasswordMatches), uow)
            .Handle(new ChangePasswordCommand(1, new ChangePasswordRequest("current-password", newPassword)), CancellationToken.None);

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => verifyResult;
    }

    private sealed class FakeUserRepository(bool userExists) : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
            Task.FromResult(userExists ? new User { Id = id, PasswordHash = "hashed" } : null);

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
