using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Auth.Repositories;

public interface IPasswordResetTokenRepository : IRepository
{
    /// <summary>依雜湊值取 token 與所屬使用者（tracked，重設時要一起改密碼），找不到回傳 null。</summary>
    Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>某帳號還沒用過的 token（tracked，寄新信或重設成功時要刪除）。</summary>
    Task<List<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken ct);

    /// <summary>由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(PasswordResetToken token);

    /// <summary>實際刪除，不是軟刪除：被取代的 token 沒有保留的價值。由呼叫端以 IUnitOfWork 提交。</summary>
    void Remove(PasswordResetToken token);
}

public sealed class PasswordResetTokenRepository(AppDbContext db) : IPasswordResetTokenRepository
{
    public async Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
        => await db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<List<PasswordResetToken>> GetUnusedByUserAsync(int userId, CancellationToken ct)
        => await db.PasswordResetTokens
            .Where(t => t.UserId == userId && t.UsedOn == null)
            .ToListAsync(ct);

    public void Add(PasswordResetToken token) => db.PasswordResetTokens.Add(token);

    public void Remove(PasswordResetToken token) => db.PasswordResetTokens.Remove(token);
}
