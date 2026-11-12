using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Addresses.Repositories;
using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Commands;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;
using Xunit;

namespace Konbini.Tests.Unit;

public class CreateOrderHandlerTests
{
    private const int ProductId = 1;

    private static CreateOrderRequest ValidRequest() => new(
        Items: [new CreateOrderItemDto(ProductId, 1)],
        ContactName: "王小明",
        ContactPhone: "0912345678",
        DeliveryMethod: 1,
        CityCode: 100,
        DistrictCode: 100,
        StreetAddress: "中正路 1 號",
        Memo: null);

    private static CreateOrderRequest WithLength(string field, int length) =>
        WithValue(field, new string('x', length));

    private static CreateOrderRequest WithValue(string field, string value)
    {
        return field switch
        {
            "contactName" => ValidRequest() with { ContactName = value },
            "contactPhone" => ValidRequest() with { ContactPhone = value },
            "streetAddress" => ValidRequest() with { StreetAddress = value },
            "memo" => ValidRequest() with { Memo = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field)),
        };
    }

    [Theory]
    [InlineData("contactName", Order.ContactNameMaxLength)]
    [InlineData("contactPhone", Order.ContactPhoneMaxLength)]
    [InlineData("streetAddress", Order.StreetAddressMaxLength)]
    [InlineData("memo", Order.MemoMaxLength)]
    public async Task FieldAtLimit_IsAccepted(string field, int limit)
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(WithLength(field, limit), uow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Theory]
    [InlineData("contactName", Order.ContactNameMaxLength)]
    [InlineData("contactPhone", Order.ContactPhoneMaxLength)]
    [InlineData("streetAddress", Order.StreetAddressMaxLength)]
    [InlineData("memo", Order.MemoMaxLength)]
    public async Task FieldOverLimit_ReturnsFieldError_WithoutSaving(string field, int limit)
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(WithLength(field, limit + 1), uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains(field, result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Theory]
    [InlineData("contactName", Order.ContactNameMaxLength)]
    [InlineData("contactPhone", Order.ContactPhoneMaxLength)]
    [InlineData("streetAddress", Order.StreetAddressMaxLength)]
    [InlineData("memo", Order.MemoMaxLength)]
    public async Task FieldAtLimitWithSurroundingSpaces_IsAccepted(string field, int limit)
    {
        var uow = new FakeUnitOfWork();

        var result = await Handle(WithValue(field, "  " + new string('x', limit) + "  "), uow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task TextFields_AreTrimmed_AndBlankMemoSavedAsNull()
    {
        var uow = new FakeUnitOfWork();
        var orders = new FakeOrderRepository();
        var request = ValidRequest() with
        {
            ContactName = "  王小明  ",
            ContactPhone = " 0912345678 ",
            StreetAddress = "  中正路 1 號 ",
            Memo = "   ",
        };

        var result = await Handle(request, uow, orders);

        Assert.True(result.IsSuccess);
        var order = Assert.IsType<Order>(orders.Added);
        Assert.Equal("王小明", order.ContactName);
        Assert.Equal("0912345678", order.ContactPhone);
        Assert.Equal("中正路 1 號", order.StreetAddress);
        Assert.Equal("臺北市中正區中正路 1 號", order.DeliveryAddress);
        Assert.Null(order.Memo);
    }

    [Fact]
    public async Task NullItem_ReturnsItemsError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = [null!] };   // 原本 NullReferenceException，回 500

        var result = await Handle(request, uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal("商品資料不正確", result.Error!.Errors["items"]);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task ItemsAtLimit_IsAccepted()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = ItemsOfCount(Order.MaxItems) };

        var result = await Handle(request, uow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task ItemsOverLimit_ReturnsItemsError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = ItemsOfCount(Order.MaxItems + 1) };

        var result = await Handle(request, uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Equal($"購物車最多 {Order.MaxItems} 項商品", result.Error!.Errors["items"]);
        Assert.Equal(0, uow.SaveCount);
    }

    /// <summary>同一商品各 1 件，合計不會碰到 MaxQuantity，只測筆數上限。</summary>
    private static List<CreateOrderItemDto> ItemsOfCount(int count) =>
        Enumerable.Range(0, count).Select(_ => new CreateOrderItemDto(ProductId, 1)).ToList();

    [Theory]
    [InlineData(OrderItem.MaxQuantity + 1)]
    [InlineData(int.MaxValue)]   // 原本會讓金額溢位
    public async Task QuantityOverLimit_ReturnsItemsError_WithoutSaving(int quantity)
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = [new CreateOrderItemDto(ProductId, quantity)] };

        var result = await Handle(request, uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("items", result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task QuantityAtLimit_IsAccepted()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = [new CreateOrderItemDto(ProductId, OrderItem.MaxQuantity)] };

        var result = await Handle(request, uow);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task SameProductSplitAcrossItems_OverLimitInTotal_ReturnsItemsError()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with
        {
            Items = [new CreateOrderItemDto(ProductId, OrderItem.MaxQuantity), new CreateOrderItemDto(ProductId, 1)],
        };

        var result = await Handle(request, uow);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("items", result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task UnknownProduct_ReturnsItemsValidationError_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var request = ValidRequest() with { Items = [new CreateOrderItemDto(ProductId + 1, 1)] };

        var result = await Handle(request, uow);

        // body 裡的參照錯誤回 400，不是 404
        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("items", result.Error!.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<Result<CreateOrderResponse>> Handle(
        CreateOrderRequest request, FakeUnitOfWork uow, FakeOrderRepository? orders = null) =>
        new CreateOrderHandler(orders ?? new FakeOrderRepository(), new FakeProductRepository(), new FakeAddressRepository(), uow)
            .Handle(new CreateOrderCommand(1, request), CancellationToken.None);

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? Added { get; private set; }

        public void Add(Order order) => Added = order;

        public Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
            Task.FromResult(new Dictionary<int, ProductSnapshot>
            {
                [ProductId] = new(ProductId, "商品", 100, null),
            });

        public Task<PagedResult<ProductDto>> GetListAsync(int type, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeAddressRepository : IAddressRepository
    {
        public Task<string?> GetCityNameAsync(int cityCode, CancellationToken ct) => Task.FromResult<string?>("臺北市");
        public Task<string?> GetDistrictNameAsync(int cityCode, int districtCode, CancellationToken ct) => Task.FromResult<string?>("中正區");

        public Task<List<CityDto>> GetCitiesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<List<DistrictDto>> GetDistrictsAsync(int cityCode, CancellationToken ct) => throw new NotSupportedException();
    }
}
