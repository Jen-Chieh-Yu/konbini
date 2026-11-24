using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Orders.Commands;
using Konbini.Api.Features.Orders.Errors;
using Konbini.Api.Features.Orders.Models;
using Konbini.Api.Features.Orders.Repositories;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;
using Xunit;

namespace Konbini.Tests.Unit;

public class CancelOrderHandlerTests
{
    private const int OwnerId = 7;
    private const int OtherUserId = 8;
    private const int OrderId = 42;

    private static OrderCancelSnapshot Pending(params OrderStockLine[] items) => new(OrderStatus.Pending, [.. items]);

    private static Task<Result> Handle(
        FakeOrderRepository orders, FakeProductRepository products, FakeUnitOfWork uow, int userId = OwnerId) =>
        new CancelOrderHandler(orders, products, uow).Handle(new(userId, OrderId), default);

    [Fact]
    public async Task PendingOrder_IsCancelledAndRestocked()
    {
        var orders = new FakeOrderRepository(Pending(new(3, 2), new(1, 5)));
        var products = new FakeProductRepository();
        var uow = new FakeUnitOfWork();

        var result = await Handle(orders, products, uow);

        Assert.True(result.IsSuccess);
        Assert.True(orders.Cancelled);
        Assert.True(uow.Committed);
        // 依商品 Id 由小到大歸還，異動者是下單的本人
        Assert.Equal([(1, 5, OwnerId), (3, 2, OwnerId)], products.Restocked);
    }

    [Fact]
    public async Task SameProductInSeveralLines_IsRestockedOnce()
    {
        var orders = new FakeOrderRepository(Pending(new(1, 2), new(1, 3)));
        var products = new FakeProductRepository();

        await Handle(orders, products, new FakeUnitOfWork());

        Assert.Equal([(1, 5, OwnerId)], products.Restocked);
    }

    [Fact]
    public async Task OthersOrder_ReturnsNotFound()
    {
        var products = new FakeProductRepository();
        var uow = new FakeUnitOfWork();

        var result = await Handle(new FakeOrderRepository(Pending(new OrderStockLine(1, 1))), products, uow, OtherUserId);

        Assert.Same(OrderErrors.NotFound, result.Error);
        Assert.False(uow.Began);
        Assert.Empty(products.Restocked);
    }

    [Fact]
    public async Task MissingOrder_ReturnsNotFound()
    {
        var products = new FakeProductRepository();
        var uow = new FakeUnitOfWork();

        var result = await Handle(new FakeOrderRepository(null), products, uow);

        Assert.Same(OrderErrors.NotFound, result.Error);
        Assert.False(uow.Began);
        Assert.Empty(products.Restocked);
    }

    [Fact]
    public async Task AlreadyCancelled_ReturnsConflictWithoutRestock()
    {
        var orders = new FakeOrderRepository(new(OrderStatus.Cancelled, [new(1, 1)]));
        var products = new FakeProductRepository();
        var uow = new FakeUnitOfWork();

        var result = await Handle(orders, products, uow);

        Assert.Same(OrderErrors.AlreadyCancelled, result.Error);
        Assert.False(uow.Began);
        Assert.Empty(products.Restocked);
    }

    [Fact]
    public async Task CancelledMeanwhile_ReturnsConflictWithoutRestock()
    {
        var orders = new FakeOrderRepository(Pending(new OrderStockLine(1, 1))) { CancelledMeanwhile = true };
        var products = new FakeProductRepository();
        var uow = new FakeUnitOfWork();

        var result = await Handle(orders, products, uow);

        Assert.Same(OrderErrors.AlreadyCancelled, result.Error);
        Assert.False(uow.Committed);
        Assert.Empty(products.Restocked);
    }

    /// <summary>照 OrderRepository 的查詢條件：訂單 Id 與擁有者都符合才找得到。</summary>
    private sealed class FakeOrderRepository(OrderCancelSnapshot? snapshot) : IOrderRepository
    {
        /// <summary>模擬「讀出之後被另一次取消搶先」：條件式 UPDATE 改不到。</summary>
        public bool CancelledMeanwhile { get; init; }

        public bool Cancelled { get; private set; }

        public Task<OrderCancelSnapshot?> GetCancelSnapshotAsync(int id, int userId, CancellationToken ct) =>
            Task.FromResult(id == OrderId && userId == OwnerId ? snapshot : null);

        public Task<bool> TryCancelAsync(int id, int userId, CancellationToken ct)
        {
            Cancelled = !CancelledMeanwhile;
            return Task.FromResult(Cancelled);
        }

        public Task<PagedResult<OrderDto>> GetByUserAsync(int userId, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<OrderDto?> GetByIdAsync(int id, int userId, CancellationToken ct) => throw new NotSupportedException();
        public void Add(Order order) => throw new NotSupportedException();
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<(int Id, int Quantity, int? ModifiedBy)> Restocked { get; } = [];

        public Task RestockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct)
        {
            Restocked.Add((id, quantity, modifiedBy));
            return Task.CompletedTask;
        }

        public Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> TryDeductStockAsync(int id, int quantity, int? modifiedBy, CancellationToken ct) => throw new NotSupportedException();
        public Task<PagedResult<ProductDto>> GetListAsync(int type, IReadOnlyCollection<int> ids, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<PagedResult<AdminProductDto>> GetAdminListAsync(PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();
        public Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct) => throw new NotSupportedException();
        public Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public bool Began { get; private set; }
        public bool Committed { get; private set; }

        public Task SaveChangesAsync(CancellationToken ct) => throw new NotSupportedException();

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct)
        {
            Began = true;
            return Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction(this));
        }

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
}
