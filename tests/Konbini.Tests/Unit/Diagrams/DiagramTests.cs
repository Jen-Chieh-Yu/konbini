using Konbini.Api.Features.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Konbini.Tests.Unit.Diagrams;

/// <summary>
/// docs/database/ 必須與 EF 模型一致，每個資料表與欄位都要有中文說明（2026-11-18）。
/// 重新產生：設定環境變數 UPDATE_DIAGRAMS=1 後執行這個測試類別，指令見 <see cref="UpdateCommands"/>。
/// </summary>
public class DiagramTests
{
    private const string OutputDir = "docs/database";

    internal const string UpdateCommands =
        "PowerShell：$env:UPDATE_DIAGRAMS='1'; dotnet test tests/Konbini.Tests --filter FullyQualifiedName~DiagramTests; Remove-Item Env:UPDATE_DIAGRAMS\n" +
        "bash：UPDATE_DIAGRAMS=1 dotnet test tests/Konbini.Tests --filter FullyQualifiedName~DiagramTests";

    [Fact]
    public void 資料表文件與EF模型一致()
    {
        var generator = new EntityDiagramGenerator(DesignModel());
        var dir = Path.Combine(RepoRoot(), OutputDir);
        var index = generator.RenderIndex();
        var blocks = generator.RenderTableBlocks();
        var problems = new List<string>();

        // 重新產生：總表整個覆寫，資料表檔只換掉標記之間的內容；寫完照樣往下檢查，多出來的檔會被列出來
        if (Environment.GetEnvironmentVariable("UPDATE_DIAGRAMS") == "1")
        {
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "README.md"), index);
            foreach (var (file, block) in blocks)
            {
                var path = Path.Combine(dir, file);
                if (EntityDiagramGenerator.Merge(ReadOrNull(path), block) is { } merged)
                    File.WriteAllText(path, merged);
            }
        }

        if (ReadOrNull(Path.Combine(dir, "README.md"))?.TrimEnd() != index.TrimEnd())
            problems.Add("README.md：缺檔或內容過時");
        foreach (var (file, block) in blocks)
        {
            var content = ReadOrNull(Path.Combine(dir, file));
            if (content is null)
                problems.Add($"{file}：缺檔");
            else if (EntityDiagramGenerator.ExtractBlock(content) is not { } actual)
                problems.Add($"{file}：找不到自動產生的標記（可能被刪掉了），補回下面兩行再重新產生：\n  " +
                             $"{EntityDiagramGenerator.BeginMarker}\n  {EntityDiagramGenerator.EndMarker}");
            else if (actual != block)
                problems.Add($"{file}：自動產生的那段過時");
        }
        foreach (var path in Directory.Exists(dir) ? Directory.GetFiles(dir, "*.md") : [])
        {
            var file = Path.GetFileName(path);
            if (file != "README.md" && !blocks.ContainsKey(file))
                problems.Add($"{file}：資料表已不存在；手寫的「說明」需要的話搬到別處，再刪掉這個檔");
        }

        Assert.True(problems.Count == 0,
            $"{OutputDir} 與 EF 模型不一致：\n{string.Join("\n", problems)}\n" +
            $"缺檔或過時的請重新產生，和程式一起 commit：\n{UpdateCommands}");
    }

    [Fact]
    public void 每個資料表與欄位都有中文說明()
    {
        var problems = new List<string>();
        foreach (var e in DesignModel().GetEntityTypes().OrderBy(e => e.GetTableName(), StringComparer.Ordinal))
        {
            var table = e.GetTableName()!;
            Check(problems, table, e.GetComment());
            foreach (var p in e.GetProperties().OrderBy(p => p.Name, StringComparer.Ordinal))
                Check(problems, $"{table}.{p.GetColumnName()}", p.GetComment());
        }

        Assert.True(problems.Count == 0,
            "下列資料表或欄位的說明不符合規定。請在 Configuration 用 HasComment 補上「中文名稱」或「中文名稱（補充說明）」" +
            "（資料表寫在 ToTable 的第二個參數，共用欄位寫在 AppDbContext），再產生 Migration：\n" +
            string.Join("\n", problems));
    }

    [Fact]
    public void 重新產生時保留手寫的說明()
    {
        string Block(string body) => $"{EntityDiagramGenerator.BeginMarker}\n{body}\n{EntityDiagramGenerator.EndMarker}";
        var old = $"{Block("舊內容")}\n\n## 說明\n\n手寫內容\n";
        Assert.Equal($"{Block("新內容")}\n\n## 說明\n\n手寫內容\n", EntityDiagramGenerator.Merge(old, Block("新內容")));
    }

    [Fact]
    public void 標記被刪掉時不覆寫檔案() =>
        Assert.Null(EntityDiagramGenerator.Merge("只有手寫內容\n", "新內容"));

    private static void Check(List<string> problems, string name, string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            problems.Add($"{name}：沒有說明");
        else if (comment.Contains('（') && !comment.EndsWith('）'))
            problems.Add($"{name}：補充說明要放在結尾的全形括號內");
    }

    /// <summary>
    /// 設計時期模型：HasComment 與欄位型別只保留在這裡，執行時期的模型讀不到。
    /// 只建模型、不連資料庫；命名規則要和 Program.cs 的 AddDbContext 一致。
    /// </summary>
    internal static IModel DesignModel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var context = new AppDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    /// <summary>讀檔並把換行統一成 LF（Windows 的工作目錄可能是 CRLF）；檔案不存在回 null。</summary>
    private static string? ReadOrNull(string path) =>
        File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Konbini.slnx")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("找不到專案根目錄（Konbini.slnx）");
    }
}
