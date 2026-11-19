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
    public async Task NewOrder_IsPending()
    {
        var orders = new FakeOrderRepository();

        var result = await Handle(ValidRequest(), new FakeUnitOfWork(), orders);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Pending, orders.Added!.OrderStatus);
        Assert.Null(orders.Added.CancelledOn);
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

    [Fact]
    public async Task StockShort_ReturnsConflictWithRemaining_WithoutSaving()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 3 });
        var request = ValidRequest() with { Items = [new CreateOrderItemDto(ProductId, 4)] };

        var result = await Handle(request, uow, products: products);

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("下列商品庫存不足：商品1（剩 3 件）", result.Error!.Errors["items"]);
        Assert.Empty(products.Deducted);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task SoldOut_ReturnsConflictMarkedSoldOut()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 0 });

        var result = await Handle(ValidRequest(), uow, products: products);

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("下列商品庫存不足：商品1（已售完）", result.Error!.Errors["items"]);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task StockExactlyEnough_DeductsAndCommits()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 5 });
        var request = ValidRequest() with { Items = [new CreateOrderItemDto(ProductId, 5)] };

        var result = await Handle(request, uow, products: products);

        Assert.True(result.IsSuccess);
        Assert.Equal([(ProductId, 5)], products.Deducted);
        Assert.Equal(1, uow.SaveCount);
        Assert.True(uow.Committed);
    }

    [Fact]
    public async Task SameProductSplitAcrossItems_DeductsTotalOnce()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 5 });
        var request = ValidRequest() with
        {
            Items = [new CreateOrderItemDto(ProductId, 2), new CreateOrderItemDto(ProductId, 3)],
        };

        var result = await Handle(request, uow, products: products);

        Assert.True(result.IsSuccess);
        Assert.Equal([(ProductId, 5)], products.Deducted);
    }

    [Fact]
    public async Task StockTakenMeanwhile_ReturnsConflict_WithoutCommitting()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 5 });
        products.SoldOutMeanwhile.Add(ProductId);   // 讀出時還有 5 件，扣的時候已被別人買光

        var result = await Handle(ValidRequest(), uow, products: products);

        Assert.Equal(ErrorType.Conflict, result.Error?.Type);
        Assert.Equal("下列商品庫存不足：商品1（已售完）", result.Error!.Errors["items"]);
        Assert.Equal(0, uow.SaveCount);
        Assert.False(uow.Committed);
    }

    [Fact]
    public async Task MultipleProducts_DeductedInIdOrder_ItemsKeepRequestOrder()
    {
        var uow = new FakeUnitOfWork();
        var orders = new FakeOrderRepository();
        var products = new FakeProductRepository(new() { [1] = 10, [2] = 10 });
        var request = ValidRequest() with
        {
            Items = [new CreateOrderItemDto(2, 1), new CreateOrderItemDto(1, 1)],
        };

        var result = await Handle(request, uow, orders, products);

        Assert.True(result.IsSuccess);
        // 依 Id 扣，避免死結；明細仍照送來的順序
        Assert.Equal([1, 2], products.Deducted.Select(d => d.Id));
        Assert.Equal([2, 1], orders.Added!.Items.Select(i => i.ProductId));
    }

    [Fact]
    public async Task FieldErrorAndStockShort_ReturnsValidationFirst()
    {
        var uow = new FakeUnitOfWork();
        var products = new FakeProductRepository(new() { [ProductId] = 0 });
        var request = ValidRequest() with { ContactName = "" };

        var result = await Handle(request, uow, products: products);

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains("contactName", result.Error!.Errors.Keys);
        Assert.Empty(products.Deducted);
    }

    private static Task<Result<CreateOrderResponse>> Handle(
        CreateOrderRequest request,
        FakeUnitOfWork uow,
        FakeOrderRepository? orders = null,
        FakeProductRepository? products = null) =>
        new CreateOrderHandler(
                orders ?? new FakeOrderRepository(),
                products ?? new FakeProductRepository(),
                new FakeAddressRepository(),
                uow)
            .Handle(new CreateOrderCommand(1, request), CancellationToken.None);

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public bool Committed { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct) =>
            Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction(this));

        private sealed class FakeTransaction(FakeUnitOfWork owner) : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken ct)
            {
                owner.Committed = true;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Order? Added { get; private set; }

        public void Add(Order order) => Added = order;

        public Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderCancelSnapshot?> GetCancelSnapshotAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryCancelAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
    }

    /// <param name="stocks">商品 Id → 庫存；沒給時只有 ProductId 一項、庫存充足。</param>
    private sealed class FakeProductRepository(Dictionary<int, int>? stocks = null) : IProductRepository
    {
        private readonly Dictionary<int, int> _stocks = stocks ?? new() { [ProductId] = 1000 };

        /// <summary>成功扣到的（商品 Id, 件數），依呼叫順序。</summary>
        public List<(int Id, int Quantity)> Deducted { get; } = [];

        /// <summary>模擬「讀出庫存之後被別人買光」：這些商品在扣庫存時才變成 0。</summary>
        public HashSet<int> SoldOutMeanwhile { get; } = [];

        public Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
            Task.FromResult(ids.Where(_stocks.ContainsKey)
                .ToDictionary(id => id, id => new ProductSnapshot(id, $"商品{id}", 100, null, _stocks[id])));

        public Task<bool> TryDeductStockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct)
        {
            if (SoldOutMeanwhile.Contains(id))
            {
                _stocks[id] = 0;
            }
            if (!_stocks.TryGetValue(id, out var stock) || stock < quantity)
            {
                return Task.FromResult(false);
            }
            _stocks[id] = stock - quantity;
            Deducted.Add((id, quantity));
            return Task.FromResult(true);
        }

        public Task<PagedResult<ProductDto>> GetListAsync(int type, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task RestockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeAddressRepository : IAddressRepository
    {
        public Task<string?> GetCityNameAsync(int cityCode, CancellationToken ct) => Task.FromResult<string?>("臺北市");
        public Task<string?> GetDistrictNameAsync(int cityCode, int districtCode, CancellationToken ct) => Task.FromResult<string?>("中正區");

        public Task<List<CityDto>> GetCitiesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<List<DistrictDto>> GetDistrictsAsync(int cityCode, CancellationToken ct) => throw new NotSupportedException();
    }
}
