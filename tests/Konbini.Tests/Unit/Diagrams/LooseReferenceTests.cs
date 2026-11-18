using Xunit;

namespace Konbini.Tests.Unit.Diagrams;

/// <summary>
/// 只存 Id、沒有資料庫外鍵的欄位都要是刻意的（2026-11-18）。
/// 新增欄位時忘了在 EF 設定寫 HasOne／HasMany，建置與其他測試都不會發現，資料庫就少了「Id 是否存在」的檢查。
/// 清單外的欄位會失敗：決定要建外鍵，或確定只存 Id 並寫下原因；清單裡已經不存在的項目也會失敗，提醒移除。
/// </summary>
public class LooseReferenceTests
{
    /// <summary>「實體.欄位」→ 原因。「待確認」的是程式與文件都沒有記錄原因，見 CLAUDE.md §6。</summary>
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.Ordinal)
    {
        ["District.CityCode"] = "待確認：代碼表之間沒有外鍵，原因沒有記錄",
        ["Order.CityCode"] = "寄送地址已存成文字快照（delivery_address），代碼只記錄下單時選的縣市",
        ["Order.DistrictCode"] = "寄送地址已存成文字快照（delivery_address），代碼只記錄下單時選的行政區",
        ["Order.UserId"] = "待確認：沒有外鍵的原因沒有記錄",
        ["OrderItem.ProductId"] = "訂單快照：明細保存下單當時的商品資料，不 join 回商品表（database-design.md「訂單快照」）",
    };

    [Fact]
    public void 只存Id的欄位都在允許清單()
    {
        var actual = new EntityDiagramGenerator(DiagramTests.DesignModel()).LooseReferences();
        var missing = actual.Where(x => !Allowed.ContainsKey(x)).ToList();
        var stale = Allowed.Keys.Where(x => !actual.Contains(x)).Order(StringComparer.Ordinal).ToList();

        Assert.True(missing.Count == 0,
            "下列欄位只存 Id、沒有資料庫外鍵。要資料庫檢查就在 EF 設定用 HasOne／HasMany 建外鍵；" +
            "確定只存 Id，就加進 LooseReferenceTests.Allowed 並寫下原因：\n" + string.Join("\n", missing));
        Assert.True(stale.Count == 0,
            "下列允許清單項目已經不是只存 Id 的欄位（欄位已刪除或改成外鍵），請從 LooseReferenceTests.Allowed 移除：\n" +
            string.Join("\n", stale));
    }
}
