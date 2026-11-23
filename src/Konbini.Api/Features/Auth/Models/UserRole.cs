using System.Text.Json.Serialization;

namespace Konbini.Api.Features.Auth.Models;

/// <summary>
/// 使用者的角色。不是第三方平台，只有顧客與管理員兩種（DD-AUT-004）。
/// 角色不放進 JWT：後台端點每次從資料庫查（AdminAuthorizationHandler），降權馬上生效。
/// 資料庫存 smallint；API 回傳小寫字串。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<UserRole>))]
public enum UserRole : short
{
    [JsonStringEnumMemberName("customer")]
    Customer = 1,

    [JsonStringEnumMemberName("admin")]
    Admin = 2,
}
