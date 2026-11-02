using Konbini.Api.Features.AppInfo.Queries;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.AppInfo.Endpoints;

public class AppInfoEndpoints : IEndpoint
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/version").WithTags("AppInfo");

        // 不需登入：部署後與 CI 冒煙測試用來確認執行中的版本，內容只有版號與 commit
        group.MapGet("/", async (
                IQueryHandler<GetVersionQuery, VersionResponse> handler,
                CancellationToken ct)
            => Results.Ok(await handler.Handle(new(), ct)))
            .Produces<VersionResponse>();
    }
}
