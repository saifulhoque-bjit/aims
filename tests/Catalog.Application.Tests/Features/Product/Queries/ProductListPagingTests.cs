#region using

using BuildingBlocks.Pagination;
using BuildingBlocks.Pagination.Extensions;
using Catalog.Application.Features.Product.Queries;
using Common.Models;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

// SPEC: spec-61ecda
public class ProductListPagingTests
{
    #region Implementations

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(0, 0)]
    public void Normalized_TreatsNonPositivePageSizeAsTen(int pageNumber, int pageSize)
    {
        var paging = new PaginationRequest(pageNumber, pageSize).Normalized();

        Assert.Equal(new PaginationRequest(1, 10), paging);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Normalized_TreatsNonPositivePageNumberAsOne(int pageNumber)
    {
        var paging = new PaginationRequest(pageNumber, 20).Normalized();

        Assert.Equal(new PaginationRequest(1, 20), paging);
    }

    [Fact]
    public void Normalized_KeepsValidPagingAndTheDefault()
    {
        Assert.Equal(new PaginationRequest(3, 25), new PaginationRequest(3, 25).Normalized());
        Assert.Equal(new PaginationRequest(1, 1000), new PaginationRequest().Normalized());
    }

    [Fact]
    public void PagingResult_ReflectsNormalizedValues()
    {
        var result = PagingResult.Of(25, new PaginationRequest(0, 0).Normalized());

        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public void GetTotalPages_StillThrows_ForNonPositivePageSize()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PaginationRequest(1, 0).GetTotalPages(25));
    }

    [Fact]
    public void WithPaging_StillChecksSkipForOverflow()
    {
        var query = Array.Empty<int>().AsQueryable();

        Assert.Throws<OverflowException>(() => query.WithPaging(new PaginationRequest(int.MaxValue, 1000)));
    }

    [Theory]
    [InlineData(1, 0, 100, 1, 10)]
    [InlineData(1, -1, 100, 1, 10)]
    [InlineData(0, 20, 100, 1, 20)]
    [InlineData(2, 500, 100, 2, 100)]
    [InlineData(1, 1000, 100, 1, 100)]
    [InlineData(1, 50, 100, 1, 50)]
    public void ResolvePaging_NormalizesThenCaps(int pageNumber, int pageSize, int max, int expectedNumber, int expectedSize)
    {
        var paging = GetProductsQueryHandler.ResolvePaging(new PaginationRequest(pageNumber, pageSize), max);

        Assert.Equal(new PaginationRequest(expectedNumber, expectedSize), paging);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void ResolvePaging_SkipsCap_WhenMaxPageSizeIsMissingOrNotPositive(int max)
    {
        Assert.Equal(new PaginationRequest(1, 1000), GetProductsQueryHandler.ResolvePaging(new PaginationRequest(), max));
        Assert.Equal(new PaginationRequest(1, 10), GetProductsQueryHandler.ResolvePaging(new PaginationRequest(1, 0), max));
    }

    #endregion
}
