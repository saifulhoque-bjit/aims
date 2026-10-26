#region using

using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Models.Filters;
using Catalog.Domain.Entities;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class ProductListFilterTests
{
    #region Fields, Properties and Indexers

    private static readonly ProductEntity PhoneX = CreateProduct("Phone X");

    private static readonly ProductEntity Laptop = CreateProduct("Laptop Pro");

    private static readonly IQueryable<ProductEntity> Products = new[] { PhoneX, Laptop }.AsQueryable();

    #endregion

    #region Implementations

    [Theory]
    [InlineData("phone")]
    [InlineData("Phone")]
    [InlineData("PHONE")]
    [InlineData("  pHoNe x  ")]
    public void Apply_MatchesName_RegardlessOfCase(string searchText)
    {
        var result = ProductListFilter.Apply(Products, new GetAllProductsFilter(searchText, null)).ToList();

        Assert.Equal(new[] { PhoneX.Id }, result.Select(x => x.Id));
    }

    [Fact]
    public void Apply_ReturnsNothing_WhenNameDoesNotContainSearchText()
    {
        var result = ProductListFilter.Apply(Products, new GetAllProductsFilter("tablet", null)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_ReturnsAll_WhenSearchTextIsBlank()
    {
        var result = ProductListFilter.Apply(Products, new GetAllProductsFilter("   ", null)).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_CombinesSearchTextWithIds()
    {
        var onlyLaptop = new GetAllProductsFilter("phone", [Laptop.Id]);

        Assert.Empty(ProductListFilter.Apply(Products, onlyLaptop).ToList());

        var result = ProductListFilter.Apply(Products, new GetAllProductsFilter(null, [Laptop.Id])).ToList();

        Assert.Equal(new[] { Laptop.Id }, result.Select(x => x.Id));
    }

    #endregion

    #region Methods

    private static ProductEntity CreateProduct(string name) =>
        ProductEntity.Create(Guid.NewGuid(), name, "SKU", "short", "long", name.Replace(' ', '-'), 100m, null, null, null, "admin");

    #endregion
}
