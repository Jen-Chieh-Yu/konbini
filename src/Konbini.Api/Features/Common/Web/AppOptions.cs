namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 應用程式層級的設定（<c>App:*</c>）。啟動時檢查，同 <see cref="Email.EmailOptions"/>。
/// </summary>
public sealed class AppOptions
{
    /// <summary>前端的網址，信中的連結指向這裡（例：<c>http://localhost:5173</c>），結尾不含斜線。</summary>
    public required string ClientBaseUrl { get; init; }

    public static AppOptions FromConfiguration(IConfiguration config)
    {
        var url = config["App:ClientBaseUrl"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "App:ClientBaseUrl 未設定或不是完整的 http(s) 網址（例：http://localhost:5173）。預設值在 appsettings.json，容器的值在 compose 的 environment（docs/configuration.md）。");
        }
        return new AppOptions { ClientBaseUrl = url!.TrimEnd('/') };
    }
}
