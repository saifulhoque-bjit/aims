#region using

using AutoMapper;
using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Mappings;
using Catalog.Domain.Entities;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class ProductDetailBuilderTests
{
    #region Fields, Properties and Indexers

    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<CatalogMappingProfile>()).CreateMapper();

    #endregion

    #region Implementations

    [Fact]
    public void Build_ReturnsProduct_WhenProductIsUnpublished()
    {
        var product = ProductEntity.Create(Guid.NewGuid(), "Draft", "SKU-1", "Short", "Long", "draft",
            100m, null, null, null, "tester");
        Assert.False(product.Published);

        var result = ProductDetailBuilder.Build(Mapper, product, [], []);

        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Short", result.ShortDescription);
    }

    #endregion
}
