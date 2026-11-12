using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Memory;

namespace Konbini.Api.Features.Common.Configuration;

/// <summary>
/// 本機 dotnet run 讀根目錄的 .env（與 docker compose 同一份），取代 user-secrets。
/// 只在 Development、往上找得到 Konbini.slnx 時作用；插在環境變數之前，已設定的環境變數優先。
/// 決策見 CLAUDE.md §3「設定的來源」，鍵一覽見 docs/configuration.md。
/// </summary>
public static class LocalDotEnv
{
    private const string RepoMarker = "Konbini.slnx";
    private const string FileName = ".env";

    public static WebApplicationBuilder AddLocalDotEnv(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
            return builder;

        var path = FindDotEnv(builder.Environment.ContentRootPath);
        if (path is null)
            return builder;

        InsertBeforeEnvironmentVariables(builder.Configuration, ToConfiguration(Parse(File.ReadAllLines(path))));
        return builder;
    }

    /// <summary>從 startDirectory 往上找 repo 根目錄（有 Konbini.slnx），回傳其中的 .env；找不到回傳 null。</summary>
    public static string? FindDotEnv(string startDirectory)
    {
        for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
        {
            if (!File.Exists(Path.Combine(dir.FullName, RepoMarker)))
                continue;
            var path = Path.Combine(dir.FullName, FileName);
            return File.Exists(path) ? path : null;
        }
        return null;
    }

    /// <summary>
    /// 解析 KEY=VALUE：略過空行與 # 開頭的註解，去掉成對的引號；
    /// 沒加引號時「空白 + #」之後是註解（與 compose 相同）。
    /// </summary>
    public static Dictionary<string, string> Parse(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in lines)
        {
            var line = raw.Trim(); // 也會去掉 CRLF 留下的 \r
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();
            if (value.Length >= 2 && (value[0] is '\'' or '"') && value[^1] == value[0])
                value = value[1..^1];
            else if (value.IndexOf(" #", StringComparison.Ordinal) is var hash and >= 0)
                value = value[..hash].TrimEnd();

            result[key] = value;
        }
        return result;
    }

    /// <summary>
    /// 把 .env 的鍵對應成 .NET 設定鍵。連線字串的格式與 docker-compose.override.yml 相同，主機改成本機；
    /// 預設值同 compose 的 ${VAR:-konbini}。空值視為未設定，交給 Program.cs 的 fail fast 報錯。
    /// </summary>
    public static Dictionary<string, string?> ToConfiguration(IReadOnlyDictionary<string, string> env)
    {
        var config = new Dictionary<string, string?>();

        if (Value(env, "APP_DB_PASSWORD") is { } password)
        {
            var database = Value(env, "APP_DB_NAME") ?? "konbini";
            var user = Value(env, "APP_DB_USER") ?? "konbini";
            config["ConnectionStrings:DefaultConnection"] =
                $"Host=127.0.0.1;Port=5432;Database={database};Username={user};Password={password}";
        }

        if (Value(env, "JWT_SECRET") is { } jwtSecret)
            config["Jwt:Secret"] = jwtSecret;

        return config;
    }

    /// <summary>插在「不帶前綴的環境變數」之前：.env 蓋過 appsettings，環境變數與命令列參數又蓋過 .env。</summary>
    public static void InsertBeforeEnvironmentVariables(IConfigurationBuilder configuration, IDictionary<string, string?> values)
    {
        var source = new MemoryConfigurationSource { InitialData = values };
        var sources = configuration.Sources;
        for (var i = 0; i < sources.Count; i++)
        {
            if (sources[i] is EnvironmentVariablesConfigurationSource { Prefix: null or "" })
            {
                sources.Insert(i, source);
                return;
            }
        }
        sources.Add(source);
    }

    private static string? Value(IReadOnlyDictionary<string, string> env, string key)
        => env.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
}
