#region using

using Catalog.Application.Features.Product.Queries;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class DiscountBadgeTests
{
    #region Fields, Properties and Indexers

    public static TheoryData<decimal, decimal?> NoBadgeCases => new()
    {
        // 100% off clearance item - the seeded "Dell XPS 15" scenario that used to
        // throw DivideByZeroException and fail the whole admin product list.
        { 1500m, 0m },
        // No sale price at all.
        { 1500m, null },
        // Negative sale price - unvalidated input.
        { 1500m, -10m },
        // Sale price equal to or above the regular price - nothing is being discounted.
        { 1500m, 1500m },
        { 1500m, 2000m },
        // Regular price of zero with a zero sale price.
        { 0m, 0m }
    };

    #endregion

    #region Implementations

    [Theory]
    [MemberData(nameof(NoBadgeCases))]
    public void Build_ReturnsNoBadge_WhenDiscountIsNotDerivable(decimal price, decimal? salePrice)
    {
        var result = DiscountBadge.Build(price, salePrice);

        Assert.Null(result);
    }

    [Fact]
    public void Build_ReturnsBadge_WhenSalePriceIsLowerThanPrice()
    {
        var result = DiscountBadge.Build(200m, 100m);

        Assert.Equal("(-50% off)", result);
    }

    [Fact]
    public void Build_DoesNotThrow_ForZeroSalePrice()
    {
        var exception = Record.Exception(() => DiscountBadge.Build(999m, 0m));

        Assert.Null(exception);
    }

    #endregion
}
