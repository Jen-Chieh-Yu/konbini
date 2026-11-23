using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Konbini.Tests.Unit;

/// <summary>
/// 掃描所有實際註冊的路由：/api/admin 底下的每個端點都要掛 Admin policy。
/// 有人自己寫 MapGroup("/api/admin/...")、沒有經過 MapAdminGroup 時，這裡會失敗。
/// </summary>
public class AdminEndpointPolicyTests
{
    [Fact]
    public void EveryAdminEndpoint_RequiresAdminPolicy()
    {
        var adminEndpoints = MapAllEndpoints()
            .Where(e => IsAdminRoute(e.RoutePattern.RawText))
            .ToList();

        // 一個後台端點都沒有時，下面的檢查會空轉通過
        Assert.NotEmpty(adminEndpoints);
        Assert.All(adminEndpoints, e => Assert.Contains(
            e.Metadata.GetOrderedMetadata<IAuthorizeData>(), a => a.Policy == Policies.Admin));
    }

    [Fact]
    public void AdminPolicy_IsOnlyUsedUnderAdminPrefix()
    {
        // 前台的路由掛了 Admin policy，顧客就用不了；後台功能要放在 /api/admin 底下
        var misplaced = MapAllEndpoints()
            .Where(e => !IsAdminRoute(e.RoutePattern.RawText)
                && e.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => a.Policy == Policies.Admin))
            .Select(e => e.RoutePattern.RawText)
            .ToList();

        Assert.Empty(misplaced);
    }

    private static bool IsAdminRoute(string? pattern)
        => pattern is not null
           && (pattern.Equals(AdminRouteExtensions.Prefix, StringComparison.OrdinalIgnoreCase)
               || pattern.StartsWith(AdminRouteExtensions.Prefix + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 和 Program.cs 一樣用 MapEndpoints 掃描註冊，不啟動伺服器、不連資料庫。
    /// 端點參數裡的服務（handler、ICurrentUser）要先註冊，框架才不會把它們當成 request body。
    /// </summary>
    private static List<RouteEndpoint> MapAllEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddHandlers();
        builder.Services.AddScoped<ICurrentUser, CurrentUser>();
        var app = builder.Build();

        app.MapEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(d => d.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }
}
