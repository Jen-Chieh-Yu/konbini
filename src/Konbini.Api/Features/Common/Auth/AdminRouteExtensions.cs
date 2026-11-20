namespace Konbini.Api.Features.Common.Auth;

public static class AdminRouteExtensions
{
    public const string Prefix = "/api/admin";

    /// <summary>
    /// 後台的 route group：一次掛好 <c>/api/admin/{resource}</c> 前綴與 Admin policy。
    /// 後台端點一律從這裡開 group，不要自己寫 <c>MapGroup("/api/admin/...")</c>：漏掛 policy 就是任何登入的人都能用
    /// （AdminEndpointPolicyTests 會擋）。後台的程式放在各自的 feature，和前台寫在同一支 XxxEndpoints.cs。
    /// </summary>
    /// <param name="resource">資源名稱，複數名詞（例：products）。</param>
    public static RouteGroupBuilder MapAdminGroup(this IEndpointRouteBuilder app, string resource)
        => app.MapGroup($"{Prefix}/{resource}")
            .RequireAuthorization(Policies.Admin)
            .WithTags("Admin");
}
