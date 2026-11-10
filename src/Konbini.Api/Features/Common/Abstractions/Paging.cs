namespace Konbini.Api.Features.Common.Abstractions;

/// <summary>分頁參數：一律經 From 建立，超出範圍的值夾到邊界（docs/api-conventions.md「分頁」）。</summary>
public sealed class PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>頁碼上限：不夾的話 page=2147483647 算 Offset 時會溢位成負數，查詢變成 500。</summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public int Page { get; }
    public int PageSize { get; }
    public int Offset => (Page - 1) * PageSize;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public static PageRequest From(int? page, int? pageSize)
        => new(Math.Clamp(page ?? 1, 1, MaxPage), Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize));
}

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);
