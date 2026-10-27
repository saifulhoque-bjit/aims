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

    // SPEC: spec-07612b - the badge states the discount as a whole percentage.
    [Theory]
    [InlineData(100, 66.67, "(-33% off)")]
    [InlineData(300, 200, "(-33% off)")]
    [InlineData(100, 33.4, "(-67% off)")]
    [InlineData(200, 99, "(-51% off)")]
    [InlineData(1000, 995, "(-1% off)")]
    public void Build_ReturnsWholePercentage(double price, double salePrice, string expected)
    {
        var result = DiscountBadge.Build((decimal)price, (decimal)salePrice);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(100, 66.67)]
    [InlineData(300, 200)]
    [InlineData(7, 3)]
    public void Build_BadgeHasNoDecimalDigits(double price, double salePrice)
    {
        var result = DiscountBadge.Build((decimal)price, (decimal)salePrice);

        Assert.NotNull(result);
        Assert.Matches(@"^\(-\d+% off\)$", result);
    }

    [Fact]
    public void Build_DoesNotThrow_ForZeroSalePrice()
    {
        var exception = Record.Exception(() => DiscountBadge.Build(999m, 0m));

        Assert.Null(exception);
    }

    #endregion
}
