using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    /// <summary>
    /// 開始明確的交易。只呼叫 SaveChanges 的用例不需要（SaveChanges 本身就是一個交易）；
    /// 立即執行的寫入（ExecuteUpdate）要和 SaveChanges 一起成立、一起取消時才用。
    /// 沒有呼叫 CommitAsync 就 Dispose 時回滾。
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct);
}

/// <summary>IUnitOfWork 開的交易；用 <c>await using</c> 包住，離開範圍前沒有 Commit 就回滾。</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
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

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct)
        => new Transaction(await db.Database.BeginTransactionAsync(ct));

    // 包一層，Handler 與測試不必依賴 EF 的 IDbContextTransaction
    private sealed class Transaction(IDbContextTransaction inner) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken ct) => inner.CommitAsync(ct);

        public ValueTask DisposeAsync() => inner.DisposeAsync();
    }
}
