namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>
/// Minimal API 端點的自我註冊介面。
/// Program.cs 啟動時掃描組件內所有實作並呼叫 <see cref="Map"/>，
/// 新增端點只要加檔案，不需修改 Program.cs。
/// </summary>
public interface IEndpoint
{
    void Map(IEndpointRouteBuilder app);
}
