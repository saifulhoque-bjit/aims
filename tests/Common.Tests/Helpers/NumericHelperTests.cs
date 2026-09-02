#region using

using Common.Helpers;
using Xunit;

#endregion

namespace Common.Tests.Helpers;

public class NumericHelperTests
{
    #region Fields, Properties and Indexers

    public static TheoryData<double, double> NoDiscountCases => new()
    {
        // The seeded catalog scenario - a "100% off" clearance item whose sale
        // price is 0 used to throw DivideByZeroException for the whole request.
        { 1500d, 0d },
        // No sale price / caller passing 0 for both.
        { 0d, 0d },
        // Zero or negative original price - nothing to discount against.
        { 0d, 500d },
        { -100d, 50d },
        // Negative sale price - unvalidated input.
        { 1500d, -10d },
        // Sale at or above the original price - nothing is being discounted.
        { 1500d, 1500d },
        { 1500d, 2000d }
    };

    #endregion

    #region Implementations

    [Theory]
    [MemberData(nameof(NoDiscountCases))]
    public void CalculateDiscountPercent_ReturnsZero_WhenDiscountIsNotDerivable(
        double originalPrice,
        double salePrice)
    {
        var result = NumericHelper.CalculateDiscountPercent(originalPrice, salePrice);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateDiscountPercent_ComputesDiscount_WhenSalePriceIsLowerThanOriginal()
    {
        var result = NumericHelper.CalculateDiscountPercent(200d, 100d);

        Assert.Equal(50, result);
    }

    [Fact]
    public void CalculateDiscountPercent_TruncatesFractionalPercentages()
    {
        var result = NumericHelper.CalculateDiscountPercent(300d, 100d);

        Assert.Equal(66, result);
    }

    [Fact]
    public void CalculateDiscountPercent_DoesNotThrow_ForZeroSalePrice()
    {
        var exception = Record.Exception(() => NumericHelper.CalculateDiscountPercent(999d, 0d));

        Assert.Null(exception);
    }

    #endregion
}
