using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
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

        Assert.True(result.Success);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task NewPasswordOverLimit_ReturnsNewPasswordError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(PasswordOfLength(PasswordPolicy.MaxLength + 1), uow);

        Assert.False(result.Success);
        Assert.Contains("newPassword", result.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<AuthResult> Handle(string newPassword, FakeUnitOfWork uow) =>
        new ChangePasswordHandler(new FakeUserRepository(), new FakePasswordHasher(), uow)
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

    /// <summary>目前密碼一律驗證通過，測試只看新密碼的規則。</summary>
    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => true;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
            Task.FromResult<User?>(new User { Id = id, PasswordHash = "hashed" });

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
