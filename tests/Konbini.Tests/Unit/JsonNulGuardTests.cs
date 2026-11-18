using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Konbini.Api.Features.Common.Web;
using Konbini.Api.Features.Orders.Models;
using Xunit;

namespace Konbini.Tests.Unit;

public class JsonNulGuardTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver().WithAddedModifier(JsonNulGuard.RejectNulInStrings),
    };

    /// <summary>JSON 裡的跳脫寫法，反序列化後才是 NUL。</summary>
    private const string Nul = @"\u0000";

    [Fact]
    public void NulInStringProperty_Throws()
    {
        var ex = Assert.Throws<JsonException>(() => Deserialize<Sample>($$"""{"name":"a{{Nul}}b"}"""));
        Assert.Contains("name", ex.Message);
    }

    [Fact]
    public void NulInNullableStringProperty_Throws()
    {
        Assert.Throws<JsonException>(() => Deserialize<Sample>($$"""{"name":"a","memo":"{{Nul}}"}"""));
    }

    [Fact]
    public void NulInNestedObject_Throws()
    {
        Assert.Throws<JsonException>(() => Deserialize<Sample>($$$"""{"name":"a","child":{"note":"{{{Nul}}}"}}"""));
    }

    [Fact]
    public void NulInCreateOrderRequest_Throws()
    {
        var json = $$"""
            {"items":[{"productId":1,"quantity":1}],"contactName":"王{{Nul}}明","contactPhone":"0912345678",
             "deliveryMethod":1,"cityCode":1,"districtCode":1,"streetAddress":"中正路 1 號","memo":null}
            """;
        Assert.Throws<JsonException>(() => Deserialize<CreateOrderRequest>(json));
    }

    [Fact]
    public void StringsWithoutNul_AreDeserialized()
    {
        // 其他控制字元 PostgreSQL 存得進去，不擋
        var sample = Deserialize<Sample>("""{"name":"辛拉麵\u0001","memo":null,"child":{"note":"白色"}}""");

        Assert.Equal(new Sample("辛拉麵\u0001", null, new Child("白色")), sample);
    }

    [Fact]
    public void Schema_KeepsStringType()
    {
        // 改用自訂 JsonConverter<string> 的話，這裡會變成 true（不限型別），OpenAPI 文件就看不出是字串
        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(Options, typeof(Sample));

        Assert.Equal("string", schema["properties"]!["name"]!["type"]!.GetValue<string>());
    }

    private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    public sealed record Sample(string Name, string? Memo, Child? Child = null);

    public sealed record Child(string Note);
}
