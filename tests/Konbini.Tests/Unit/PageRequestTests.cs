using Konbini.Api.Features.Common.Abstractions;
using Xunit;

namespace Konbini.Tests.Unit;

public class PageRequestTests
{
    [Fact]
    public void From_WithoutValues_UsesDefaults()
    {
        var paging = PageRequest.From(null, null);

        Assert.Equal(1, paging.Page);
        Assert.Equal(PageRequest.DefaultPageSize, paging.PageSize);
        Assert.Equal(0, paging.Offset);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(int.MaxValue, PageRequest.MaxPage)]
    public void From_PageOutOfRange_IsClamped(int page, int expected)
    {
        Assert.Equal(expected, PageRequest.From(page, null).Page);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1000, PageRequest.MaxPageSize)]
    public void From_PageSizeOutOfRange_IsClamped(int pageSize, int expected)
    {
        Assert.Equal(expected, PageRequest.From(null, pageSize).PageSize);
    }

    [Fact]
    public void Offset_AtLargestValues_DoesNotOverflow()
    {
        var paging = PageRequest.From(int.MaxValue, int.MaxValue);

        Assert.True(paging.Offset >= 0);
    }
}
