using Konbini.Api.Features.Auth.Models;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Auth.Repositories;

public interface IEmailVerificationTokenRepository : IRepository
{
    /// <summary>依雜湊值取 token 與所屬使用者（tracked，驗證時要一起改），找不到回傳 null。</summary>
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    /// <summary>某帳號還沒用過的 token（tracked，重寄時要刪除）。</summary>
    Task<List<EmailVerificationToken>> GetUnusedByUserAsync(int userId, CancellationToken ct);

    /// <summary>由呼叫端以 IUnitOfWork 提交。</summary>
    void Add(EmailVerificationToken token);

    /// <summary>實際刪除，不是軟刪除：被新的驗證信取代的 token 沒有保留的價值。由呼叫端以 IUnitOfWork 提交。</summary>
    void Remove(EmailVerificationToken token);
}

public sealed class EmailVerificationTokenRepository(AppDbContext db) : IEmailVerificationTokenRepository
{
    public async Task<EmailVerificationToken?> GetByHashAsync(string tokenHash, CancellationToken ct)
        => await db.EmailVerificationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<List<EmailVerificationToken>> GetUnusedByUserAsync(int userId, CancellationToken ct)
        => await db.EmailVerificationTokens
            .Where(t => t.UserId == userId && t.UsedOn == null)
            .ToListAsync(ct);

    public void Add(EmailVerificationToken token) => db.EmailVerificationTokens.Add(token);

    public void Remove(EmailVerificationToken token) => db.EmailVerificationTokens.Remove(token);
}
