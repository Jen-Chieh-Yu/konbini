using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Products.Queries;

/// <param name="Type">商品類別；0 = 全部</param>
public record GetProductsQuery(int Type);

public class GetProductsHandler(AppDbContext db)
    : IQueryHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery query, CancellationToken ct)
        => await db.Products.AsNoTracking()
            .Where(p => query.Type == 0 || p.Type == query.Type)
            .OrderBy(p => p.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);
}
