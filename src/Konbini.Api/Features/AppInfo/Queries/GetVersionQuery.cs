using System.Reflection;
using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.AppInfo.Queries;

public record GetVersionQuery;

/// <summary>執行中的版號與 commit。發行版由 CI 依標籤寫入；本機建置為 0.0.0-dev。</summary>
public record VersionResponse(string Version, string Commit);

public class GetVersionHandler : IQueryHandler<GetVersionQuery, VersionResponse>
{
    // 版號在建置時寫進組件，執行期間不會變，啟動後讀一次即可
    private static readonly VersionResponse Current = Read();

    public Task<VersionResponse> Handle(GetVersionQuery query, CancellationToken ct)
        => Task.FromResult(Current);

    private static VersionResponse Read()
    {
        // InformationalVersion 的格式是「版號+commit」（Dockerfile 的 /p:InformationalVersion）
        var info = typeof(GetVersionHandler).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0-dev";
        var parts = info.Split('+', 2);
        return new VersionResponse(parts[0], parts.Length > 1 ? parts[1] : "unknown");
    }
}
