using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Konbini.Api.Features.Common.Web;

/// <summary>
/// 請求的 JSON 字串含 NUL（\u0000）時視為格式錯誤：PostgreSQL 的文字欄位不接受 NUL，
/// 放行的話會在寫入或查詢時變成 500（PostgresException 22021）。
/// 拋出的 JsonException 由框架轉成 BadHttpRequestException，經 GlobalExceptionHandler 回 400。
/// <para>
/// 用 TypeInfoResolver 的 modifier 在物件反序列化完成後檢查，不用自訂 JsonConverter&lt;string&gt;：
/// 自訂 converter 會讓 OpenAPI 文件的所有字串欄位失去 "type": "string"。
/// 只檢查物件（含巢狀物件）的 string 屬性；List&lt;string&gt;、Dictionary 的值、整個 body 是 string 時不檢查，
/// 目前沒有這類請求，出現時再補。
/// </para>
/// </summary>
public static class JsonNulGuard
{
    public static void RejectNulInStrings(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var stringProperties = typeInfo.Properties
            .Where(p => p.PropertyType == typeof(string) && p.Get is not null)
            .ToArray();
        if (stringProperties.Length == 0)
            return;

        var previous = typeInfo.OnDeserialized;
        typeInfo.OnDeserialized = obj =>
        {
            foreach (var property in stringProperties)
            {
                if (property.Get!(obj) is string value && value.Contains('\0'))
                    throw new JsonException($"欄位 {property.Name} 不可包含 NUL 字元。");
            }
            previous?.Invoke(obj);
        };
    }
}
