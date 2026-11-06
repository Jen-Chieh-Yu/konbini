using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;
using Xunit;

namespace Konbini.Tests.Unit;

public class RegisterHandlerTests
{
    private static RegisterRequest ValidRequest() => new(
        LastName: "王",
        FirstName: "小明",
        Email: "user@example.com",
        Password: "password123",
        ConfirmPassword: "password123",
        PhoneNumber: "0912345678",
        Year: 2000,
        Month: 1,
        Day: 1);

    private static string EmailOfLength(int length)
    {
        const string domain = "@example.com";
        return new string('a', length - domain.Length) + domain;
    }

    [Fact]
    public async Task NameAtLimit_IsAccepted()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with
        {
            LastName = new string('姓', 50),
            FirstName = new string('名', User.NameMaxLength - 50),
        };

        var result = await Handle(request, uow);

        Assert.True(result.Success);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task NameOverLimit_ReturnsNameError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with
        {
            LastName = new string('姓', 50),
            FirstName = new string('名', User.NameMaxLength - 50 + 1),
        };

        var result = await Handle(request, uow);

        Assert.False(result.Success);
        Assert.Contains("name", result.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task EmailAtLimit_IsAccepted()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(ValidRequest() with { Email = EmailOfLength(User.EmailMaxLength) }, uow);

        Assert.True(result.Success);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task EmailOverLimit_ReturnsEmailError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(ValidRequest() with { Email = EmailOfLength(User.EmailMaxLength + 1) }, uow);

        Assert.False(result.Success);
        Assert.Contains("email", result.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<AuthResult> Handle(RegisterRequest request, FakeUnitOfWork uow) =>
        new RegisterHandler(new FakeUserRepository(), new FakePasswordHasher(), uow)
            .Handle(new RegisterCommand(request), CancellationToken.None);

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => true;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => Task.FromResult(false);
        public void Add(User user) { }

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
