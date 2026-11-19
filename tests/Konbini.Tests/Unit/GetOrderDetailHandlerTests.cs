using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Orders.Errors;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Queries;
using Konbini.Api.Features.Orders.Repositories;
using Xunit;

namespace Konbini.Tests.Unit;

public class GetOrderDetailHandlerTests
{
    private const int OwnerId = 7;
    private const int OtherUserId = 8;
    private const int OrderId = 42;

    private static readonly OrderDto OwnOrder = new(
        OrderId, 100, 60, 160, "王小明", "0912345678", "臺北市中正區中正路 1 號", null,
        new DateTime(2026, 11, 16, 6, 5, 0, DateTimeKind.Utc), OrderStatus.Pending, null,
        [new OrderItemDto(1, "商品", 100, 1, 100, null)]);

    private static Task<Result<OrderDto>> Handle(int userId, int orderId) =>
        new GetOrderDetailHandler(new FakeOrderRepository()).Handle(new(userId, orderId), default);

    [Fact]
    public async Task OwnOrder_ReturnsOrder()
    {
        var result = await Handle(OwnerId, OrderId);

        Assert.True(result.IsSuccess);
        Assert.Same(OwnOrder, result.Value);
    }

    [Fact]
    public async Task OtherUsersOrder_ReturnsNotFound()
    {
        var result = await Handle(OtherUserId, OrderId);

        Assert.Same(OrderErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task MissingOrder_ReturnsNotFound()
    {
        var result = await Handle(OwnerId, OrderId + 1);

        Assert.Same(OrderErrors.NotFound, result.Error);
    }

    /// <summary>照 OrderRepository 的查詢條件回傳：訂單 Id 與擁有者都符合才找得到。</summary>
    private sealed class FakeOrderRepository : IOrderRepository
    {
        public Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct) =>
            Task.FromResult(id == OrderId && userId == OwnerId ? OwnOrder : null);

        public Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public void Add(Order order) => throw new NotSupportedException();
        public Task<OrderCancelSnapshot?> GetCancelSnapshotAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryCancelAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
    }
}
