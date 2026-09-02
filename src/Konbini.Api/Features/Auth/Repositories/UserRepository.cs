using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Auth.Repositories;

public interface IUserRepository : IRepository
{
    /// <summary>依 Email 取使用者（唯讀，Login 驗密碼用；entity 只在 feature 內流動）。</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct);

    /// <summary>取 tracked entity 供修改，配合 IUnitOfWork 提交。</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>目前使用者的公開資料投影。</summary>
    Task<UserDto?> GetProfileAsync(int id, CancellationToken ct);

    /// <summary>加入新使用者；由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(User user);
}

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct)
        => await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct)
        => await db.Users.AnyAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdAsync(int id, CancellationToken ct)
        => await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<UserDto?> GetProfileAsync(int id, CancellationToken ct)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UserDto(u.Id, u.Name, u.Email, u.Phone))
            .FirstOrDefaultAsync(ct);

    public void Add(User user) => db.Users.Add(user);
}
