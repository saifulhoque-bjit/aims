#region using

using Catalog.Application.Features.Product.Commands;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Commands;

public class CreateProductSeoTitleTests
{
    #region Implementations

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveSeoTitle_UsesWholeName_WhenNameIsShorterThanLimit(string? seoTitle)
    {
        // A typical product name is far shorter than 60 characters; this used to
        // throw ArgumentOutOfRangeException and fail POST /admin/products with 500.
        var result = CreateProductCommandHandler.ResolveSeoTitle(seoTitle, "Dell XPS 15");

        Assert.Equal("Dell XPS 15", result);
    }

    [Fact]
    public void ResolveSeoTitle_UsesWholeName_WhenNameIsExactlyAtLimit()
    {
        var name = new string('a', 60);

        var result = CreateProductCommandHandler.ResolveSeoTitle(null, name);

        Assert.Equal(name, result);
    }

    [Fact]
    public void ResolveSeoTitle_CutsNameToLimit_WhenNameIsLonger()
    {
        var name = new string('a', 60) + "overflow";

        var result = CreateProductCommandHandler.ResolveSeoTitle(null, name);

        Assert.Equal(new string('a', 60), result);
    }

    [Fact]
    public void ResolveSeoTitle_KeepsExplicitSeoTitle()
    {
        var result = CreateProductCommandHandler.ResolveSeoTitle("Custom title", "Dell XPS 15");

        Assert.Equal("Custom title", result);
    }

    #endregion
}
