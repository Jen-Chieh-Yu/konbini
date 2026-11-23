using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Auth.Repositories;
using Konbini.Api.Features.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Konbini.Tests.Unit;

public class AdminAuthorizationHandlerTests
{
    private const int UserId = 7;

    [Fact]
    public async Task Admin_Succeeds()
    {
        Assert.True(await Authorize(SignedIn(UserId), UserRole.Admin));
    }

    [Fact]
    public async Task Customer_Fails()
    {
        Assert.False(await Authorize(SignedIn(UserId), UserRole.Customer));
    }

    [Fact]
    public async Task DeletedOrMissingUser_Fails()
    {
        // token 還沒過期，但帳號已軟刪除：查不到角色
        Assert.False(await Authorize(SignedIn(UserId), role: null));
    }

    [Fact]
    public async Task Anonymous_FailsWithoutQueryingDatabase()
    {
        var users = new FakeUserRepository(UserRole.Admin);

        Assert.False(await Authorize(new ClaimsPrincipal(new ClaimsIdentity()), users));
        Assert.Empty(users.RequestedIds);
    }

    [Fact]
    public async Task QueriesRoleOfTokenSubject()
    {
        var users = new FakeUserRepository(UserRole.Admin);

        await Authorize(SignedIn(UserId), users);

        Assert.Equal([UserId], users.RequestedIds);
    }

    private static ClaimsPrincipal SignedIn(int id)
        => new(new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, id.ToString())], "Bearer"));

    private static Task<bool> Authorize(ClaimsPrincipal user, UserRole? role)
        => Authorize(user, new FakeUserRepository(role));

    private static async Task<bool> Authorize(ClaimsPrincipal user, FakeUserRepository users)
    {
        var requirement = new AdminRequirement();
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);

        await new AdminAuthorizationHandler(users).HandleAsync(context);

        return context.HasSucceeded;
    }

    /// <summary>不管查哪個 Id 都回同一個角色，並記下查過的 Id。</summary>
    private sealed class FakeUserRepository(UserRole? role) : IUserRepository
    {
        public List<int> RequestedIds { get; } = [];

        public Task<UserRole?> GetRoleAsync(int id, CancellationToken ct)
        {
            RequestedIds.Add(id);
            return Task.FromResult(role);
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> EmailExistsAsync(string email, CancellationToken ct) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<UserDto?> GetProfileAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public void Add(User user) => throw new NotSupportedException();
    }
}
