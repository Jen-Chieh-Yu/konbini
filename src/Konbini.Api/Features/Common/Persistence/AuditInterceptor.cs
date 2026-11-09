using Konbini.Api.Features.Common.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Konbini.Api.Features.Common.Persistence;

/// <summary>寫入前填稽核欄位。放攔截器而非 UnitOfWork：種子資料不經過 UnitOfWork。</summary>
public sealed class AuditInterceptor(ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public void Stamp(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var userId = currentUser.IdOrNull;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedOn = now;
                entry.Entity.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                // 建立資訊寫入後不再變動
                entry.Property(e => e.CreatedOn).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
            }
            else
            {
                continue;
            }

            entry.Entity.ModifiedOn = now;
            entry.Entity.ModifiedBy = userId;
        }
    }
}
