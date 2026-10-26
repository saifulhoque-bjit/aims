#region using

using AutoMapper;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Mappings;
using Catalog.Domain.Entities;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class ProductCategoriesTests
{
    #region Fields, Properties and Indexers

    private readonly IMapper _mapper = new MapperConfiguration(cfg => cfg.AddProfile<CatalogMappingProfile>()).CreateMapper();

    private static readonly CategoryEntity Phones = CategoryEntity.Create(Guid.NewGuid(), "Phones", string.Empty, "Phones", "admin");

    private static readonly CategoryEntity Laptops = CategoryEntity.Create(Guid.NewGuid(), "Laptops", string.Empty, "Laptops", "admin");

    #endregion

    #region Implementations

    [Fact]
    public void Fill_ListsEachCategoryIdOnce_AfterAutoMapperCopiedThem()
    {
        // The admin product list maps ProductEntity -> ProductDto first; AutoMapper copies
        // CategoryIds by convention, and the old loop appended them again (["a1", "a1"]).
        var product = CreateProduct([Phones.Id]);
        var item = _mapper.Map<ProductDto>(product);

        ProductCategories.Fill(item, product.CategoryIds, [Phones, Laptops]);

        Assert.Equal(new[] { Phones.Id }, item.CategoryIds);
        Assert.Equal(new[] { "Phones" }, item.CategoryNames);
    }

    [Fact]
    public void Fill_KeepsIdsAndNamesInStep_ForSeveralCategories()
    {
        var product = CreateProduct([Phones.Id, Laptops.Id]);
        var item = _mapper.Map<ProductDto>(product);

        ProductCategories.Fill(item, product.CategoryIds, [Phones, Laptops]);

        Assert.Equal(new[] { Phones.Id, Laptops.Id }, item.CategoryIds);
        Assert.Equal(new[] { "Phones", "Laptops" }, item.CategoryNames);
    }

    [Fact]
    public void Fill_ListsCategoryOnce_WhenStoredIdsHoldADuplicate()
    {
        var product = CreateProduct([Phones.Id, Phones.Id]);
        var item = _mapper.Map<ProductDto>(product);

        ProductCategories.Fill(item, product.CategoryIds, [Phones]);

        Assert.Equal(new[] { Phones.Id }, item.CategoryIds);
        Assert.Equal(new[] { "Phones" }, item.CategoryNames);
    }

    [Fact]
    public void Fill_DropsUnknownCategoryIds_SoIdsMatchNames()
    {
        var product = CreateProduct([Guid.NewGuid(), Phones.Id]);
        var item = _mapper.Map<ProductDto>(product);

        ProductCategories.Fill(item, product.CategoryIds, [Phones]);

        Assert.Equal(new[] { Phones.Id }, item.CategoryIds);
        Assert.Equal(new[] { "Phones" }, item.CategoryNames);
    }

    [Fact]
    public void Fill_LeavesEmptyIdsAndNoNames_WhenProductHasNoCategories()
    {
        var product = CreateProduct(null);
        var item = _mapper.Map<ProductDto>(product);

        ProductCategories.Fill(item, product.CategoryIds, [Phones]);

        Assert.NotNull(item.CategoryIds);
        Assert.Empty(item.CategoryIds);
        Assert.Null(item.CategoryNames);
    }

    private static ProductEntity CreateProduct(List<Guid>? categoryIds)
        => ProductEntity.Create(Guid.NewGuid(), "Phone X", "SKU-1", "Short", "Long", "Phone-X", 100m, null, categoryIds, null, "admin");

    #endregion
}
