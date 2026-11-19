using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Errors;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Orders.Commands;

/// <summary>UserId 取自 token，不接受前端傳來的值。</summary>
public record CancelOrderCommand(int UserId, int Id);

/// <summary>
/// 取消訂單並歸還庫存。只有下單的本人、只有處理中的訂單能取消。
/// 改狀態與歸還庫存在同一個交易；狀態用條件式 UPDATE 改，同時送出的兩次取消只有一次成功，庫存不會還兩次。
/// </summary>
public class CancelOrderHandler(
    IOrderRepository orders,
    IProductRepository products,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand command, CancellationToken ct)
    {
        // 不存在與不是自己的都回 404，不讓人分辨訂單是否存在（同訂單明細）
        var order = await orders.GetCancelSnapshotAsync(command.Id, command.UserId, ct);
        if (order is null)
        {
            return OrderErrors.NotFound;
        }

        // 先用讀出的狀態檢查：大部分的重複取消在這裡就回 409，不必開交易
        if (order.Status != OrderStatus.Pending)
        {
            return OrderErrors.AlreadyCancelled;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        // 讀出之後被另一次取消搶先時，條件式 UPDATE 改不到；不 Commit，離開時回滾
        if (!await orders.TryCancelAsync(command.Id, command.UserId, ct))
        {
            return OrderErrors.AlreadyCancelled;
        }

        // 依商品 Id 由小到大加回去，和下單扣庫存的順序相同，同時執行時才不會互相等待而死結。
        // 下單時同一個商品已經合併成一筆；這裡再合併一次，不依賴這個前提
        foreach (var line in order.Items.GroupBy(i => i.ProductId).OrderBy(g => g.Key))
        {
            await products.RestockAsync(line.Key, line.Sum(i => i.Quantity), command.UserId, ct);
        }

        await transaction.CommitAsync(ct);
        return Result.Success();
    }
}
