#region using

#endregion

namespace Catalog.Application.Features.Product.Queries;

/// <summary>
///     Builds the "-x% off" badge appended to a product's short description on the
///     admin product list. SalePrice is optional and unvalidated, so a zero or
///     non-positive sale price (or one at/above the regular price) yields no badge
///     instead of a divide-by-zero that fails the whole product list.
/// </summary>
public static class DiscountBadge
{
    #region Fields, Properties and Indexers

    /// <summary>
    ///     Returns the badge text, or <see langword="null" /> when no discount can be derived.
    /// </summary>
    public static string? Build(decimal price, decimal? salePrice)
    {
        if (salePrice is null or <= 0 || salePrice >= price)
        {
            return null;
        }

        var discountPercentage = (price - salePrice.Value) / price * 100;

        return $"(-{discountPercentage:0.##}% off)";
    }

    #endregion
}
