using Konbini.Api.Features.Addresses.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
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

        if (request.Items is null || request.Items.Count == 0)
        {
            errors["items"] = "購物車是空的";
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

        if (string.IsNullOrWhiteSpace(request.ContactName))
        {
            errors["contactName"] = "請填寫聯絡人姓名";
        }
        else if (request.ContactName.Length > Order.ContactNameMaxLength)
        {
            errors["contactName"] = $"聯絡人姓名不可超過 {Order.ContactNameMaxLength} 個字元";
        }

        if (string.IsNullOrWhiteSpace(request.ContactPhone))
        {
            errors["contactPhone"] = "請填寫聯絡電話";
        }
        else if (request.ContactPhone.Length > Order.ContactPhoneMaxLength)
        {
            errors["contactPhone"] = $"聯絡電話不可超過 {Order.ContactPhoneMaxLength} 個字元";
        }

        if (string.IsNullOrWhiteSpace(request.StreetAddress))
        {
            errors["streetAddress"] = "請填寫地址";
        }
        else if (request.StreetAddress.Length > Order.StreetAddressMaxLength)
        {
            errors["streetAddress"] = $"地址不可超過 {Order.StreetAddressMaxLength} 個字元";
        }

        if (request.Memo?.Length > Order.MemoMaxLength)
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
            // body 裡參照到不存在的商品屬於輸入錯誤，回 400；404 只用在網址指到的資源
            return Error.Validation("items", $"下列商品不存在：{string.Join(", ", missing)}");
        }

        var items = request.Items!
            .GroupBy(i => i.ProductId)
            .Select(g =>
            {
                var product = snapshots[g.Key];
                var quantity = g.Sum(i => i.Quantity);
                return new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = quantity,
                    Subtotal = product.Price * quantity,
                    ImageUrl = product.ImageUrl,
                };
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
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            DeliveryMethod = request.DeliveryMethod,
            CityCode = request.CityCode,
            DistrictCode = request.DistrictCode,
            StreetAddress = request.StreetAddress,
            DeliveryAddress = cityName + districtName + request.StreetAddress,
            Memo = request.Memo,
            Items = items,
        };

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateOrderResponse(order.Id);
    }
}
