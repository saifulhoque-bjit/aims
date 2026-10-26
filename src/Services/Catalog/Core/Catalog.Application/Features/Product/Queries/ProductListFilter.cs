#region using

using Catalog.Application.Models.Filters;
using Catalog.Domain.Entities;

#endregion

namespace Catalog.Application.Features.Product.Queries;

/// <summary>
///     Applies the admin product list filter (GET all products): the trimmed SearchText
///     matches anywhere in the product name regardless of letter case, and Ids restricts
///     the result to the given products.
/// </summary>
public static class ProductListFilter
{
    #region Fields, Properties and Indexers

    public static IQueryable<ProductEntity> Apply(IQueryable<ProductEntity> productQuery, GetAllProductsFilter filter)
    {
        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            var search = filter.SearchText.Trim();
            productQuery = productQuery.Where(x => x.Name != null && x.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        if (filter.Ids?.Length > 0)
        {
            productQuery = productQuery.Where(x => filter.Ids.Contains(x.Id));
        }

        return productQuery;
    }

    #endregion
}
