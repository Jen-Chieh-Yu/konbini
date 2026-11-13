using Konbini.Api.Features.Common.Abstractions;

namespace Konbini.Api.Features.Products.Errors;

/// <summary>Products 的業務錯誤。</summary>
public static class ProductErrors
{
    public static readonly Error NotFound = Error.NotFound("找不到商品");
}
