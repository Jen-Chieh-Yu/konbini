using Konbini.Api.Features.Auth.Commands;
using Konbini.Api.Features.Auth.Errors;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Xunit;

namespace Konbini.Tests.Unit;

public class LoginHandlerTests
{
    private const string Email = "user@example.com";

    [Fact]
    public async Task CorrectPassword_ReturnsToken()
    {
        var result = await Handle(new LoginRequest(Email, "Password!23"), passwordMatches: true);

        Assert.True(result.IsSuccess);
        Assert.Equal("token", result.Value.Token);
        Assert.Equal(Email, result.Value.User.Email);
    }

    [Fact]
    public async Task WrongPassword_ReturnsUnauthorized()
    {
        var result = await Handle(new LoginRequest(Email, "Wrong!234"), passwordMatches: false);

        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task UnknownEmail_ReturnsSameUnauthorizedAsWrongPassword()
    {
        var result = await Handle(new LoginRequest("nobody@example.com", "Password!23"), passwordMatches: true);

        // 帳號不存在與密碼錯誤回同一個結果，避免帳號列舉
        Assert.Same(AuthErrors.InvalidCredentials, result.Error);
    }

    [Fact]
    public async Task BlankPassword_ReturnsValidationError()
    {
        var result = await Handle(new LoginRequest(Email, " "), passwordMatches: true);

        Assert.Same(AuthErrors.CredentialsRequired, result.Error);
    }

    private static Task<Result<LoginResponse>> Handle(LoginRequest request, bool passwordMatches) =>
        new LoginHandler(new FakeUserRepository(), new FakePasswordHasher(passwordMatches), new FakeTokenService())
            .Handle(new LoginCommand(request), CancellationToken.None);

    private sealed class FakePasswordHasher(bool verifyResult) : IPasswordHasher
    {
        public string Hash(string password) => "hashed";
        public bool Verify(string password, string hash) => verifyResult;
    }

    private sealed class FakeTokenService : IJwtTokenService
    {
        public string CreateToken(User user) => "token";
    }

    /// <summary>只有 <see cref="Email"/> 這個帳號存在。</summary>
    private sealed class FakeUserRepository : IUserRepository
    {
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
            Task.FromResult(email == Email ? new User { Id = 1, Name = "王小明", Email = Email, PasswordHash = "hashed" } : null);

        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
    }
}
