namespace Common.Helpers;

/// <summary>
///     Discount percentage helper. Prices are unvalidated input, so a zero or
///     non-positive original price yields no discount instead of a divide-by-zero
///     that fails the caller's request.
/// </summary>
public static class NumericHelper
{
    #region Methods

    public static int CalculateDiscountPercent(double originalPrice, double salePrice)
    {
        if (originalPrice <= 0 || salePrice <= 0 || salePrice >= originalPrice)
        {
            return 0;
        }

        double discountAmount = originalPrice - salePrice;
        double discountPercent = (discountAmount / originalPrice) * 100;
        return (int)discountPercent;
    }

    #endregion
}