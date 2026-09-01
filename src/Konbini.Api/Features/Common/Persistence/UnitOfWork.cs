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

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
