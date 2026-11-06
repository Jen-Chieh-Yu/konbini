using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Konbini.Api.Features.Common.Persistence;

/// <summary>
/// 交易邊界：Repository 只改變狀態（Add、修改 tracked entity），
/// 由 Handler 在用例結束時呼叫 SaveChangesAsync 統一提交。
/// 同一 HTTP scope 共用同一個 AppDbContext，跨 repository 的變更
/// 會在同一次 SaveChanges（同一交易）內落地。
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

/// <summary>寫入違反唯一索引。由 UnitOfWork 翻譯，Handler 不必依賴資料庫驅動的例外型別。</summary>
public sealed class UniqueConstraintViolationException(Exception innerException)
    : Exception("寫入違反唯一索引。", innerException);

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new UniqueConstraintViolationException(ex);
        }
    }
}
