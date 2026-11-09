using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Common.Auth;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Konbini.Tests.Unit;

public class AuditInterceptorTests
{
    // 只用 ChangeTracker 與模型，不會連線
    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused")
            .UseSnakeCaseNamingConvention()
            .Options);

    private static User NewUser() => new() { Name = "王小明", Email = "user@example.com", PasswordHash = "hash" };

    [Fact]
    public void Added_FillsCreatedAndModified()
    {
        using var db = CreateContext();
        var user = NewUser();
        db.Users.Add(user);

        new AuditInterceptor(new FakeCurrentUser(7)).Stamp(db);

        Assert.Equal(DateTimeKind.Utc, user.CreatedOn.Kind);
        Assert.Equal(user.CreatedOn, user.ModifiedOn);
        Assert.Equal(7, user.CreatedBy);
        Assert.Equal(7, user.ModifiedBy);
    }

    [Fact]
    public void Added_WithoutLogin_LeavesUserIdNull()
    {
        using var db = CreateContext();
        var user = NewUser();
        db.Users.Add(user);

        new AuditInterceptor(new FakeCurrentUser(null)).Stamp(db);

        Assert.Null(user.CreatedBy);
        Assert.Null(user.ModifiedBy);
    }

    [Fact]
    public void Modified_UpdatesModified_AndKeepsCreated()
    {
        using var db = CreateContext();
        var createdOn = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = NewUser();
        user.Id = 1;
        user.CreatedOn = createdOn;
        user.CreatedBy = 3;
        db.Users.Attach(user);
        user.Name = "新名字";

        new AuditInterceptor(new FakeCurrentUser(7)).Stamp(db);

        var entry = db.Entry(user);
        Assert.False(entry.Property(u => u.CreatedOn).IsModified);
        Assert.False(entry.Property(u => u.CreatedBy).IsModified);
        Assert.Equal(7, user.ModifiedBy);
        Assert.True(user.ModifiedOn > createdOn);
    }

    /// <summary>改了 Entity 卻忘記產生 Migration 時，讓 CI 失敗。</summary>
    [Fact]
    public void Model_HasNoPendingChanges()
    {
        using var db = CreateContext();

        Assert.False(db.Database.HasPendingModelChanges());
    }

    private sealed class FakeCurrentUser(int? id) : ICurrentUser
    {
        public int Id => id ?? throw new UnauthorizedAccessException();
        public int? IdOrNull => id;
    }
}
