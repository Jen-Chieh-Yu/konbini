using Konbini.Api.Features.Addresses.Models;
using Konbini.Api.Features.Addresses.Repositories;
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

    private static CreateOrderRequest WithLength(string field, int length)
    {
        var value = new string('x', length);
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

        Assert.True(result.Success);
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

        Assert.False(result.Success);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Equal(0, uow.SaveCount);
    }

    private static Task<CreateOrderResult> Handle(CreateOrderRequest request, FakeUnitOfWork uow) =>
        new CreateOrderHandler(new FakeOrderRepository(), new FakeProductRepository(), new FakeAddressRepository(), uow)
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
        public void Add(Order order) { }

        public Task<List<OrderDto>> GetByUserAsync(int userId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
            Task.FromResult(new Dictionary<int, ProductSnapshot>
            {
                [ProductId] = new(ProductId, "商品", 100, null),
            });

        public Task<List<ProductDto>> GetListAsync(int type, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ProductDto>> SearchAsync(string[] keywords, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeAddressRepository : IAddressRepository
    {
        public Task<string?> GetCityNameAsync(int cityCode, CancellationToken ct) => Task.FromResult<string?>("臺北市");
        public Task<string?> GetDistrictNameAsync(int cityCode, int districtCode, CancellationToken ct) => Task.FromResult<string?>("中正區");

        public Task<List<CityDto>> GetCitiesAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<List<DistrictDto>> GetDistrictsAsync(int cityCode, CancellationToken ct) => throw new NotSupportedException();
    }
}
