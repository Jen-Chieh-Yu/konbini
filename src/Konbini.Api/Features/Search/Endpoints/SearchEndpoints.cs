using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Search.Queries;

namespace Konbini.Api.Features.Search.Endpoints;

public class SearchEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", async (
                string keyword,
                int? page,
                int? pageSize,
                IQueryHandler<SearchProductsQuery, PagedResult<ProductDto>> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(keyword, PageRequest.From(page, pageSize)), ct)))
            .WithTags("Search")
            .Produces<PagedResult<ProductDto>>();
    }
}
