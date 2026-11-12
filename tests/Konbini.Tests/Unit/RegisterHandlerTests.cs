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
        Password: "Password!23",
        ConfirmPassword: "Password!23",
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
    public async Task NameAtLimitWithSurroundingSpaces_IsAccepted()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with
        {
            LastName = "  " + new string('姓', 50) + " ",
            FirstName = " " + new string('名', User.NameMaxLength - 50) + "  ",
        };

        var result = await Handle(request, uow);

        Assert.True(result.Success);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task BlankLastName_ReturnsLastNameError()
    {
        var result = await Handle(ValidRequest() with { LastName = "   " }, new FakeUnitOfWork());

        Assert.False(result.Success);
        Assert.Contains("lastName", result.Errors.Keys);
    }

    [Fact]
    public async Task FieldsAreTrimmed_AndEmailNormalizedBeforeValidation()
    {
        var uow = new FakeUnitOfWork();
        var users = new FakeUserRepository();
        var request = ValidRequest() with
        {
            LastName = "  王 ",
            FirstName = " 小明  ",
            Email = "  User@Example.com ",   // 原本用原字串驗格式，前後有空白就回「Email格式不正確」
            PhoneNumber = " 0912345678 ",
        };

        var result = await Handle(request, uow, users);

        Assert.True(result.Success);
        Assert.Equal("user@example.com", users.CheckedEmail);
        var user = Assert.IsType<User>(users.Added);
        Assert.Equal("王小明", user.Name);
        Assert.Equal("user@example.com", user.Email);
        Assert.Equal("0912345678", user.Phone);
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

    [Fact]
    public async Task EmailTakenByConcurrentRequest_ReturnsEmailError()
    {
        var uow = new FakeUnitOfWork { ThrowOnSave = new UniqueConstraintViolationException(new Exception()) };

        var result = await Handle(ValidRequest(), uow);

        Assert.False(result.Success);
        Assert.Contains("email", result.Errors.Keys);
    }

    [Fact]
    public async Task PasswordOverLimit_ReturnsPasswordError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var password = "Aa1!" + new string('a', PasswordPolicy.MaxLength - 3);

        var result = await Handle(ValidRequest() with { Password = password, ConfirmPassword = password }, uow);

        Assert.False(result.Success);
        Assert.Contains("password", result.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<AuthResult> Handle(RegisterRequest request, FakeUnitOfWork uow, FakeUserRepository? users = null) =>
        new RegisterHandler(users ?? new FakeUserRepository(), new FakePasswordHasher(), uow)
            .Handle(new RegisterCommand(request), CancellationToken.None);

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Exception? ThrowOnSave { get; init; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            if (ThrowOnSave is not null) throw ThrowOnSave;
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
        public string? CheckedEmail { get; private set; }
        public User? Added { get; private set; }

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        {
            CheckedEmail = email;
            return Task.FromResult(false);
        }

        public void Add(User user) => Added = user;

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
