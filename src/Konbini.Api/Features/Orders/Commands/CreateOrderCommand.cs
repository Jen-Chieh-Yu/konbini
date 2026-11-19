using Konbini.Api.Features.Addresses.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Errors;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;
using Konbini.Api.Features.Products.Repositories;

namespace Konbini.Api.Features.Orders.Commands;

public record CreateOrderCommand(int UserId, CreateOrderRequest Request);

/// <summary>建立成功只回訂單 Id；明細之後由 Location 指向的路由取得。</summary>
public record CreateOrderResponse(int Id);

/// <summary>
/// 建立訂單。購物車狀態在前端（Pinia），這裡收到的是商品 Id 與數量；
/// 單價、小計、運費一律以資料庫現價重新計算——金額裁決權在後端。
/// 庫存也由後端判斷：扣庫存與建立訂單在同一個交易內，任何一項扣不到，整筆訂單不成立。
/// </summary>
public class CreateOrderHandler(
    IOrderRepository orders,
    IProductRepository products,
    IAddressRepository addresses,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var request = command.Request;
        var errors = new Dictionary<string, string>();
        // 先去頭尾空白再驗證，長度與儲存都用處理後的值；只有空白的備註視為沒填
        var contactName = request.ContactName?.Trim() ?? "";
        var contactPhone = request.ContactPhone?.Trim() ?? "";
        var streetAddress = request.StreetAddress?.Trim() ?? "";
        var memo = string.IsNullOrWhiteSpace(request.Memo) ? null : request.Memo.Trim();

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = "購物車是空的";
        }
        else if (request.Items.Count > Order.MaxItems)
        {
            errors["items"] = $"購物車最多 {Order.MaxItems} 項商品";
        }
        else if (request.Items.Any(i => i is null))
        {
            errors["items"] = "商品資料不正確";
        }
        else if (request.Items.Any(i => i.Quantity <= 0))
        {
            errors["items"] = "商品數量必須大於 0";
        }
        // 先擋單筆再加總：單筆都在上限內，Sum 才不會溢位；同商品分多筆送來時看合計
        else if (request.Items.Any(i => i.Quantity > OrderItem.MaxQuantity)
            || request.Items.GroupBy(i => i.ProductId).Any(g => g.Sum(i => i.Quantity) > OrderItem.MaxQuantity))
        {
            errors["items"] = $"每項商品數量不可超過 {OrderItem.MaxQuantity} 件";
        }

        if (contactName.Length == 0)
        {
            errors["contactName"] = "請填寫聯絡人姓名";
        }
        else if (contactName.Length > Order.ContactNameMaxLength)
        {
            errors["contactName"] = $"聯絡人姓名不可超過 {Order.ContactNameMaxLength} 個字元";
        }

        if (contactPhone.Length == 0)
        {
            errors["contactPhone"] = "請填寫聯絡電話";
        }
        else if (contactPhone.Length > Order.ContactPhoneMaxLength)
        {
            errors["contactPhone"] = $"聯絡電話不可超過 {Order.ContactPhoneMaxLength} 個字元";
        }

        if (streetAddress.Length == 0)
        {
            errors["streetAddress"] = "請填寫地址";
        }
        else if (streetAddress.Length > Order.StreetAddressMaxLength)
        {
            errors["streetAddress"] = $"地址不可超過 {Order.StreetAddressMaxLength} 個字元";
        }

        if (memo?.Length > Order.MemoMaxLength)
        {
            errors["memo"] = $"備註不可超過 {Order.MemoMaxLength} 個字元";
        }

        var cityName = await addresses.GetCityNameAsync(request.CityCode, ct);
        var districtName = await addresses.GetDistrictNameAsync(request.CityCode, request.DistrictCode, ct);
        if (cityName is null || districtName is null)
        {
            errors["address"] = "縣市或行政區不正確";
        }

        if (errors.Count > 0)
        {
            return Error.Validation(errors);
        }

        var productIds = request.Items!.Select(i => i.ProductId).Distinct().ToList();
        var snapshots = await products.GetSnapshotsAsync(productIds, ct);

        var missing = productIds.Where(id => !snapshots.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            return OrderErrors.ProductsNotFound(missing);
        }

        // 同一個商品分多筆送來時合併；保持送來的順序，明細 Id 也依這個順序
        var lines = request.Items!
            .GroupBy(i => i.ProductId)
            .Select(g => (Product: snapshots[g.Key], Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        // 先用剛讀出的庫存檢查：大部分的庫存不足在這裡就回 409，不必開交易
        var shortages = lines.Where(l => l.Product.Stock < l.Quantity).Select(l => l.Product).ToList();
        if (shortages.Count > 0)
        {
            return OrderErrors.InsufficientStock(shortages);
        }

        var items = lines
            .Select(l => new OrderItem
            {
                ProductId = l.Product.Id,
                ProductName = l.Product.Name,
                UnitPrice = l.Product.Price,
                Quantity = l.Quantity,
                Subtotal = l.Product.Price * l.Quantity,
                ImageUrl = l.Product.ImageUrl,
            })
            .ToList();

        var subtotal = items.Sum(i => i.Subtotal);
        var deliveryFee = Pricing.CalculateDeliveryFee(subtotal);

        var order = new Order
        {
            UserId = command.UserId,
            Subtotal = subtotal,
            DeliveryFee = deliveryFee,
            Total = subtotal + deliveryFee,
            ContactName = contactName,
            ContactPhone = contactPhone,
            DeliveryMethod = request.DeliveryMethod,
            CityCode = request.CityCode,
            DistrictCode = request.DistrictCode,
            StreetAddress = streetAddress,
            DeliveryAddress = cityName + districtName + streetAddress,
            Memo = memo,
            Items = items,
        };

        // 扣庫存與建立訂單在同一個交易：任何一項扣不到，已經扣掉的也一起回滾
        await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        // 讀出庫存之後被別人買走時，條件式 UPDATE 會扣不到。依商品 Id 由小到大扣：
        // 兩筆訂單同時扣多項商品時，才不會各自鎖住一項、等待對方而死結
        var failedIds = new List<int>();
        foreach (var (product, quantity) in lines.OrderBy(l => l.Product.Id))
        {
            if (!await products.TryDeductStockAsync(product.Id, quantity, command.UserId, ct))
            {
                failedIds.Add(product.Id);
            }
        }

        if (failedIds.Count > 0)
        {
            // 不 Commit，離開時回滾。重新讀一次，訊息才是現在剩下的件數；這段時間被刪除的商品回「不存在」
            var latest = await products.GetSnapshotsAsync(failedIds, ct);
            var deleted = failedIds.Where(id => !latest.ContainsKey(id)).ToList();
            return deleted.Count > 0
                ? OrderErrors.ProductsNotFound(deleted)
                : OrderErrors.InsufficientStock(failedIds.Select(id => latest[id]));
        }

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CreateOrderResponse(order.Id);
    }
}
