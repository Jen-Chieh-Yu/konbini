using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Persistence;
using Konbini.Api.Features.Products.Models;
using Microsoft.EntityFrameworkCore;

namespace Konbini.Api.Features.Products.Queries;

public record GetProductDetailQuery(int Id);

public class GetProductDetailHandler(AppDbContext db)
    : IQueryHandler<GetProductDetailQuery, ProductDetailDto?>
{
    public async Task<ProductDetailDto?> Handle(GetProductDetailQuery query, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .FirstOrDefaultAsync(ct);

        if (product is null)
        {
            return null;
        }

        var relevant = await db.Products.AsNoTracking()
            .Where(p => p.Type == product.Type && p.Id != product.Id)
            .Select(p => new ProductDto(p.Id, p.Type, p.Name, p.Price, p.ImageUrl))
            .ToListAsync(ct);

        return new ProductDetailDto(product, relevant);
    }
}
