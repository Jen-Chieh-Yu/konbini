using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Konbini.Tests.Unit.Diagrams;

/// <summary>
/// 從 AppDbContext 的設計時期模型產生 docs/database/（2026-11-18）：總表 README.md，以及每張資料表一個檔。
/// 資料表檔的上半部（兩行標記之間）自動產生，下半部「說明」手寫，重新產生時只換掉標記之間的內容。
/// 中文名稱與說明取自 Configuration 的 HasComment，格式是「中文名稱」或「中文名稱（補充說明）」。
/// 輸出必須穩定（每次產生結果相同），所以所有清單都明確排序，不依賴 EF 回傳的順序。
/// 比照另一專案的同名產生器；這裡只有一個 DbContext，拿掉聚合分組與總覽。
/// </summary>
internal sealed class EntityDiagramGenerator
{
    internal const string BeginMarker =
        "<!-- 自動產生開始：這段請勿手動修改，改了 Entity 或 EF 設定後重新產生（README「修改資料表結構」） -->";
    internal const string EndMarker = "<!-- 自動產生結束：以下的「說明」手寫，重新產生不會蓋掉 -->";
    private const string HandWrittenHint =
        "<!-- 手寫：聚合邊界、為什麼沒有外鍵、為什麼存快照。業務規則寫在 docs/modules/，這裡用連結指過去。 -->";

    /// <summary>
    /// 一條關聯：<paramref name="From"/> 的 <paramref name="Property"/> 存著 <paramref name="To"/> 的主鍵。
    /// <paramref name="IsForeignKey"/>＝資料庫外鍵（實線），否則只存 Id、沒有外鍵（虛線）。
    /// </summary>
    private sealed record Relation(
        IEntityType From, IEntityType To, IProperty Property, bool IsForeignKey, bool Required, bool Unique);

    /// <summary>Mermaid 設定：和流程圖同一組綠色；SVG 文字避免 Rider 預覽的樣式蓋掉文字顏色。</summary>
    private const string MermaidInit =
        """%%{init: {"theme": "base", "htmlLabels": false, "themeVariables": {"fontSize": "14px", "primaryColor": "#ccfbf1", "primaryBorderColor": "#0f766e", "primaryTextColor": "#134e4a", "textColor": "#1f2937", "lineColor": "#4b5563", "attributeBackgroundColorOdd": "#ffffff", "attributeBackgroundColorEven": "#f0fdfa"}}}%%""";

    private readonly IReadOnlyList<IEntityType> _entities;
    private readonly IReadOnlyList<Relation> _relations;

    public EntityDiagramGenerator(IModel model)
    {
        _entities = model.GetEntityTypes()
            .OrderBy(e => e.GetTableName(), StringComparer.Ordinal)
            .ToList();

        var relations = new List<Relation>();
        foreach (var e in _entities)
        {
            // 資料庫外鍵：目前都是單一欄位
            foreach (var fk in e.GetForeignKeys())
                relations.Add(new Relation(e, fk.PrincipalEntityType, fk.Properties[0], true, fk.IsRequired, fk.IsUnique));

            // 只存 Id：不是主鍵也不是外鍵，但名稱對得到其他資料表的主鍵
            foreach (var p in e.GetProperties().Where(p => !p.IsPrimaryKey() && !p.IsForeignKey()))
            {
                if (ResolveLooseReference(e, p) is { } target)
                    relations.Add(new Relation(e, target, p, false, !p.IsNullable, false));
            }
        }

        _relations = relations
            .OrderBy(r => r.To.GetTableName(), StringComparer.Ordinal)
            .ThenBy(r => r.From.GetTableName(), StringComparer.Ordinal)
            .ThenBy(r => r.Property.GetColumnName(), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 欄位名稱是「實體名稱＋Id」（ProductId → Product），或等於代碼表的主鍵名稱（CityCode → City）時，
    /// 視為只存 Id 的參照。CreatedBy、ModifiedBy 也存會員編號，但名稱不符合，所以不畫線
    /// （每張表都有，畫了整張圖都會連到 users）。
    /// </summary>
    private IEntityType? ResolveLooseReference(IEntityType owner, IProperty p) =>
        _entities.FirstOrDefault(t => t != owner
            && t.FindPrimaryKey()?.Properties is [var pk]
            && p.Name == (pk.Name == "Id" ? t.ClrType.Name + "Id" : pk.Name));

    /// <summary>只存 Id、沒有外鍵的欄位，以「實體.欄位」表示（LooseReferenceTests 比對允許清單）。</summary>
    public IReadOnlyList<string> LooseReferences() =>
        _relations.Where(r => !r.IsForeignKey)
            .Select(r => $"{r.From.ClrType.Name}.{r.Property.Name}")
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>總表 docs/database/README.md 的完整內容（整個檔都是自動產生）。</summary>
    public string RenderIndex()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# 資料表");
        sb.AppendLine();
        sb.AppendLine("> 自動產生，請勿手動修改。改了 Entity 或 EF 設定後重新產生，指令見 [README「修改資料表結構」](../../README.md#修改資料表結構ef-migrations)。");
        sb.AppendLine("> 資料來源是 `AppDbContext` 的 EF Core 模型，中文名稱與說明取自 Configuration 的 `HasComment()`；規範見 [database-design.md](../database-design.md)。");
        sb.AppendLine();
        sb.AppendLine("每張資料表一個檔：上半部自動產生（說明、相鄰的關聯、欄位），下半部的「說明」手寫，記錄聚合邊界、沒有外鍵或存快照的理由；業務規則寫在 [docs/modules](../modules/README.md)。");
        sb.AppendLine();
        sb.AppendLine("| 資料表 | 中文名稱 | 說明 |");
        sb.AppendLine("|---|---|---|");
        foreach (var e in _entities)
        {
            var (name, detail) = Split(e.GetComment());
            sb.AppendLine($"| [{e.GetTableName()}]({FileName(e)}) | {Cell(name)} | {Cell(detail)} |");
        }
        sb.AppendLine();
        AppendLegend(sb);
        sb.AppendLine("## 關聯圖");
        sb.AppendLine();
        AppendDiagram(sb, _entities, _relations);
        return sb.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>每張資料表的檔名（例：orders.md）→ 自動產生的那段（含前後標記）。</summary>
    public IReadOnlyDictionary<string, string> RenderTableBlocks() =>
        _entities.ToDictionary(FileName, RenderTableBlock, StringComparer.Ordinal);

    private string RenderTableBlock(IEntityType e)
    {
        var (name, detail) = Split(e.GetComment());
        var related = _relations.Where(r => r.From == e || r.To == e).ToList();
        var sb = new StringBuilder();
        sb.AppendLine(BeginMarker);
        sb.AppendLine($"# {e.GetTableName()} {name}");
        sb.AppendLine();
        if (detail.Length > 0)
        {
            sb.AppendLine($"> {detail}");
            sb.AppendLine();
        }
        sb.AppendLine($"實體：`{e.ClrType.Name}`。[← 回到資料表總表](README.md)");
        sb.AppendLine();
        sb.AppendLine("## 關聯");
        sb.AppendLine();
        if (related.Count == 0)
        {
            sb.AppendLine("沒有關聯。");
            sb.AppendLine();
        }
        else
        {
            // 這張表與直接相連的表；相鄰表的方框只列主鍵與畫了線的欄位
            var involved = related.SelectMany(r => new[] { r.From, r.To }).Distinct()
                .OrderBy(x => x.GetTableName(), StringComparer.Ordinal).ToList();
            AppendDiagram(sb, involved, related);
            sb.AppendLine("| 方向 | 資料表 | 欄位 | 種類 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var r in related.OrderBy(r => r.From == e ? 0 : 1))
            {
                var other = r.From == e ? r.To : r.From;
                sb.AppendLine($"| {(r.From == e ? "參照" : "被參照")} | [{other.GetTableName()}]({FileName(other)}) {Cell(Split(other.GetComment()).Name)} | `{r.From.GetTableName()}.{r.Property.GetColumnName()}` | {(r.IsForeignKey ? "外鍵" : "只存 Id")} |");
            }
            sb.AppendLine();
        }
        sb.AppendLine("## 欄位");
        sb.AppendLine();
        sb.AppendLine("鍵：PK＝主鍵、FK＝資料庫外鍵、只存 Id＝存著其他資料表的主鍵，但沒有外鍵。索引：唯一＝唯一索引、有＝一般索引；主鍵本身就有索引，不另外標。");
        sb.AppendLine();
        sb.AppendLine("| 屬性 | 中文名稱 | 資料庫欄位 | 鍵 | 型別 | 必填 | 索引 | 說明 |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var p in OrderedProperties(e))
        {
            var (col, colDetail) = Split(p.GetComment());
            sb.AppendLine($"| {p.Name} | {Cell(col)} | `{p.GetColumnName()}` | {KeyText(e, p)} | {p.GetColumnType()} | {(p.IsNullable ? "" : "是")} | {IndexText(p)} | {Cell(colDetail)} |");
        }
        sb.AppendLine();
        sb.Append(EndMarker);
        return sb.ToString().ReplaceLineEndings("\n");
    }

    /// <summary>
    /// 把自動產生的那段放進資料表檔：新檔加上空的「說明」，既有檔只換掉兩行標記之間的內容。
    /// 既有檔找不到標記時回 null，不覆寫（裡面可能有手寫內容）。
    /// </summary>
    internal static string? Merge(string? existing, string block)
    {
        if (existing is null) return $"{block}\n\n## 說明\n\n{HandWrittenHint}\n";
        return FindBlock(existing) is { } r ? existing[..r.Start] + block + existing[r.End..] : null;
    }

    /// <summary>取出兩行標記之間（含標記）的內容；標記不見了回 null。</summary>
    internal static string? ExtractBlock(string content) => FindBlock(content) is { } r ? content[r] : null;

    private static Range? FindBlock(string content)
    {
        var start = content.IndexOf(BeginMarker, StringComparison.Ordinal);
        if (start < 0) return null;
        var end = content.IndexOf(EndMarker, start, StringComparison.Ordinal);
        return end < 0 ? null : start..(end + EndMarker.Length);
    }

    private static void AppendLegend(StringBuilder sb)
    {
        sb.AppendLine("## 怎麼看關聯圖");
        sb.AppendLine();
        sb.AppendLine("| 看哪裡 | 意思 |");
        sb.AppendLine("|---|---|");
        sb.AppendLine("| 實線 | 資料庫外鍵：資料庫會檢查 Id 是否存在 |");
        sb.AppendLine("| 虛線 | 只存 Id：沒有外鍵，資料庫不檢查；原因見 `LooseReferenceTests` 的允許清單 |");
        sb.AppendLine("| 線端 `‖` | 恰好一筆 |");
        sb.AppendLine("| 線端 `o│` | 零或一筆 |");
        sb.AppendLine("| 線端 `o<`（鳥爪） | 零到多筆 |");
        sb.AppendLine("| 線上文字 | 存 Id 的欄位 |");
        sb.AppendLine();
        sb.AppendLine("讀法：靠近哪個方框的符號，就是那一邊有幾筆。例如 `orders ‖──o< order_items`：一筆訂單有零到多筆明細，每筆明細屬於恰好一筆訂單。");
        sb.AppendLine("方框只列主鍵與畫了線的欄位，所有欄位見各資料表的檔案。`created_by`、`modified_by` 也存會員編號，但每張表都有，不畫線。");
        sb.AppendLine();
    }

    /// <summary>總表與相鄰關聯圖共用：方框只列主鍵與這張圖裡畫了線的欄位。</summary>
    private static void AppendDiagram(StringBuilder sb, IReadOnlyList<IEntityType> entities, IReadOnlyList<Relation> relations)
    {
        sb.AppendLine("```mermaid");
        sb.AppendLine(MermaidInit);
        sb.AppendLine("erDiagram");
        foreach (var e in entities)
        {
            sb.AppendLine($"    {e.ClrType.Name}[\"{EntityLabel(e)}\"] {{");
            var drawn = relations.Where(r => r.From == e).ToDictionary(r => r.Property);
            var shown = e.GetProperties()
                .Where(p => p.IsPrimaryKey() || drawn.ContainsKey(p))
                .OrderBy(p => p.IsPrimaryKey() ? 0 : 1)
                .ThenBy(p => p.GetColumnName(), StringComparer.Ordinal);
            foreach (var p in shown)
            {
                drawn.TryGetValue(p, out var r);
                // Mermaid 的鍵只接受 PK、FK、UK；只存 Id 改在說明前標註
                var key = (p.IsPrimaryKey(), r?.IsForeignKey == true) switch
                {
                    (true, true) => " PK, FK",
                    (true, false) => " PK",
                    (false, true) => " FK",
                    _ => "",
                };
                var name = Split(p.GetComment()).Name;
                var comment = r is { IsForeignKey: false } ? $"只存 Id：{name}" : name;
                sb.AppendLine($"        {TypeName(p.ClrType)} {p.GetColumnName()}{key} \"{Quote(comment)}\"");
            }
            sb.AppendLine("    }");
        }
        foreach (var r in relations)
        {
            // 被參照端：必填＝恰好一筆，可空＝零或一筆；參照端：唯一＝零或一筆，否則零到多筆
            var left = r.Required ? "||" : "|o";
            var right = r.Unique ? "o|" : "o{";
            var line = r.IsForeignKey ? "--" : "..";
            sb.AppendLine($"    {r.To.ClrType.Name} {left}{line}{right} {r.From.ClrType.Name} : \"{r.Property.GetColumnName()}\"");
        }
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private string KeyText(IEntityType e, IProperty p)
    {
        var parts = new List<string>();
        if (p.IsPrimaryKey()) parts.Add("PK");
        if (_relations.FirstOrDefault(r => r.From == e && r.Property == p) is { } r)
            parts.Add($"{(r.IsForeignKey ? "FK" : "只存 Id")} → [{r.To.GetTableName()}]({FileName(r.To)})");
        return string.Join("、", parts);
    }

    private static string IndexText(IProperty p)
    {
        if (p.IsPrimaryKey()) return "";
        var indexes = p.GetContainingIndexes().ToList();
        return indexes.Any(i => i.IsUnique) ? "唯一" : indexes.Count > 0 ? "有" : "";
    }

    /// <summary>主鍵在前，其餘依程式宣告順序：Entity 自己的欄位在前，AuditableEntity 的共用欄位在後。</summary>
    private static IEnumerable<IProperty> OrderedProperties(IEntityType e)
    {
        // 宣告屬性的類別與 Entity 差幾層繼承：Entity 自己宣告的是 0，AuditableEntity 是 1
        int Depth(Type? declaring)
        {
            var depth = 0;
            for (var t = e.ClrType; t is not null && t != declaring; t = t.BaseType) depth++;
            return depth;
        }

        // MetadataToken 在同一個類別內依宣告順序遞增，用來還原程式碼裡的欄位順序
        return e.GetProperties()
            .OrderBy(p => p.IsPrimaryKey() ? 0 : 1)
            .ThenBy(p => p.PropertyInfo is null ? 1 : 0)
            .ThenBy(p => Depth(p.PropertyInfo?.DeclaringType))
            .ThenBy(p => p.PropertyInfo?.MetadataToken ?? 0)
            .ThenBy(p => p.Name, StringComparer.Ordinal);
    }

    private static string FileName(IEntityType e) => $"{e.GetTableName()}.md";

    private static string EntityLabel(IEntityType e) =>
        Split(e.GetComment()).Name is { Length: > 0 } zh ? $"{e.GetTableName()} {zh}" : e.GetTableName()!;

    /// <summary>「中文名稱（補充說明）」拆成名稱與補充；沒有括號時補充是空字串。</summary>
    internal static (string Name, string Detail) Split(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment)) return ("", "");
        var open = comment.IndexOf('（');
        if (open < 0) return (comment.Trim(), "");
        var detail = comment[(open + 1)..].Trim();
        return (comment[..open].Trim(), detail.EndsWith('）') ? detail[..^1] : detail);
    }

    /// <summary>erDiagram 方框內的型別：Mermaid 只接受不含空白的一個字。</summary>
    private static string TypeName(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        if (u.IsEnum) return "enum";
        if (u == typeof(int)) return "int";
        if (u == typeof(string)) return "string";
        if (u == typeof(bool)) return "bool";
        if (u == typeof(DateTime)) return "datetime";
        if (u == typeof(DateOnly)) return "date";
        return u.Name.ToLowerInvariant();
    }

    /// <summary>Mermaid 的字串用雙引號包住，內容的雙引號換成單引號。</summary>
    private static string Quote(string s) => s.Replace("\"", "'");

    /// <summary>markdown 表格的儲存格：「|」換成全形「／」，避免破壞表格。</summary>
    private static string Cell(string s) => s.Replace("|", "／");
}
