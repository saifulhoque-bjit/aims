#region using

using Catalog.Application.Dtos.Products;
using Catalog.Domain.Entities;

#endregion

namespace Catalog.Application.Features.Product.Queries;

/// <summary>
///     Fills a listed product's CategoryIds and CategoryNames from the stored category ids.
///     AutoMapper already copies CategoryIds onto the ProductDto by convention, so both
///     lists are rebuilt here rather than appended to; otherwise every id shows up twice
///     on the admin product list. Each known category appears once, and the two lists
///     stay in step.
/// </summary>
public static class ProductCategories
{
    #region Fields, Properties and Indexers

    public static void Fill(ProductDto item, IEnumerable<Guid>? categoryIds, IReadOnlyList<CategoryEntity> categories)
    {
        item.CategoryIds = [];
        item.CategoryNames = null;

        if (categoryIds == null) return;

        foreach (var categoryId in categoryIds.Distinct())
        {
            var category = categories.FirstOrDefault(c => c.Id == categoryId);
            if (category != null)
            {
                item.CategoryNames ??= [];
                item.CategoryNames.Add(category.Name!);
                item.CategoryIds.Add(category.Id);
            }
        }
    }

    #endregion
}
