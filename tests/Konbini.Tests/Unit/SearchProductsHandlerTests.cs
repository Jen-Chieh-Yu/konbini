using Konbini.Api.Features.Common.Abstractions;
using Konbini.Api.Features.Products.Models;
using Konbini.Api.Features.Products.Repositories;
using Konbini.Api.Features.Search.Queries;
using Xunit;

namespace Konbini.Tests.Unit;

public class SearchProductsHandlerTests
{
    [Fact]
    public async Task FullWidthSpace_SplitsKeywords()
    {
        Assert.Equal(["辛拉麵", "白色"], await KeywordsPassedFor("辛拉麵　白色"));
    }

    [Fact]
    public async Task MixedWhitespace_SplitsKeywords()
    {
        Assert.Equal(["pocky", "辛拉麵", "白色"], await KeywordsPassedFor(" pocky\t辛拉麵\n  白色 "));
    }

    [Fact]
    public async Task DuplicateKeywords_AreRemovedIgnoringCase_KeepingFirstSpelling()
    {
        Assert.Equal(["pocky", "辛拉麵"], await KeywordsPassedFor("pocky Pocky POCKY 辛拉麵 辛拉麵"));
    }

    [Fact]
    public async Task Duplicates_DoNotCountTowardsLimit()
    {
        var keyword = string.Join(' ', Enumerable.Repeat("a", 20)) + " b";

        Assert.Equal(["a", "b"], await KeywordsPassedFor(keyword));
    }

    [Fact]
    public async Task OverMaxKeywords_TakesFirstMax()
    {
        // 1000 個關鍵字原本會讓 API 程序 stack overflow
        var keyword = string.Join(' ', Enumerable.Range(1, 1000).Select(i => $"a{i}"));

        var keywords = await KeywordsPassedFor(keyword);

        Assert.Equal(Enumerable.Range(1, SearchProductsHandler.MaxKeywords).Select(i => $"a{i}"), keywords);
    }

    [Fact]
    public async Task KeywordAtMaxLength_IsUnchanged()
    {
        var keyword = new string('a', SearchProductsHandler.MaxKeywordLength);

        Assert.Equal([keyword], await KeywordsPassedFor(keyword));
    }

    [Fact]
    public async Task KeywordOverMaxLength_IsTruncated()
    {
        var keywords = await KeywordsPassedFor(new string('a', SearchProductsHandler.MaxKeywordLength + 1));

        Assert.Equal([new string('a', SearchProductsHandler.MaxKeywordLength)], keywords);
    }

    [Fact]
    public async Task Truncation_DoesNotSplitSurrogatePair()
    {
        // 😀 佔兩個 UTF-16 字元，剛好跨過上限
        var prefix = new string('a', SearchProductsHandler.MaxKeywordLength - 1);

        Assert.Equal([prefix], await KeywordsPassedFor(prefix + "😀"));
    }

    [Fact]
    public async Task WhitespaceOnly_PassesNoKeywords()
    {
        Assert.Empty(await KeywordsPassedFor("　 "));
    }

    private static async Task<string[]> KeywordsPassedFor(string keyword)
    {
        var repository = new FakeProductRepository();
        await new SearchProductsHandler(repository)
            .Handle(new SearchProductsQuery(keyword, PageRequest.From(null, null)), CancellationToken.None);
        return repository.Keywords!;
    }

    /// <summary>只記下 Handler 傳進來的關鍵字，不查資料。</summary>
    private sealed class FakeProductRepository : IProductRepository
    {
        public string[]? Keywords { get; private set; }

        public Task<PagedResult<ProductDto>> SearchAsync(string[] keywords, PageRequest paging, CancellationToken ct)
        {
            Keywords = keywords;
            return Task.FromResult(new PagedResult<ProductDto>([], paging.Page, paging.PageSize, 0));
        }

        public Task<PagedResult<ProductDto>> GetListAsync(int type, PageRequest paging, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct) => throw new NotSupportedException();

        public Task<List<ProductDto>> GetRelatedAsync(int type, int excludeId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<Dictionary<int, ProductSnapshot>> GetSnapshotsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
