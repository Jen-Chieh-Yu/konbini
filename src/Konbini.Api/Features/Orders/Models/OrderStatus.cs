using System.Text.Json.Serialization;

namespace Konbini.Api.Features.Orders.Models;

/// <summary>
/// 訂單狀態（履約流程），和稽核的 EntityStatus（軟刪除）是兩件事。
/// 數值留間隔：之後的「已出貨」「已完成」插在中間，不必改既有資料。
/// 資料庫存 smallint；API 回傳小寫字串，前端與 log 看得懂。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus : short
{
    [JsonStringEnumMemberName("pending")]
    Pending = 1,

    [JsonStringEnumMemberName("cancelled")]
    Cancelled = 9,
}
