#region using

using AutoMapper;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Models.Filters;
using Catalog.Application.Models.Results;
using Catalog.Domain.Entities;
using Common.Configurations;
using Marten;
using Marten.Pagination;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

#endregion

namespace Catalog.Application.Features.Product.Queries;

public sealed record GetProductsQuery(
    GetProductsFilter Filter,
    PaginationRequest Paging) : IQuery<GetProductsResult>;

public sealed class GetProductsQueryHandler(IDocumentSession session, IMapper mapper, IConfiguration cfg)
    : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    #region Implementations

    public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        // A client may ask for any page size; never load more than the configured maximum.
        var maxPageSize = cfg.GetValue<int>($"{PagingCfg.Section}:{PagingCfg.MaxPageSize}");
        var paging = query.Paging with { PageSize = Math.Min(query.Paging.PageSize, maxPageSize) };
        var productQuery = session.Query<ProductEntity>().AsQueryable();
        Regex? relevance = null;

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            var search = filter.SearchText.Trim();
            relevance = new Regex(search, RegexOptions.IgnoreCase);
            productQuery = productQuery.Where(x => x.Name != null && x.Name.Contains(search));
        }
        if (filter.Ids?.Length > 0)
        {
            productQuery = productQuery.Where(x => filter.Ids.Contains(x.Id));
        }

        var totalCount = await productQuery.CountAsync(cancellationToken);
        var result = await productQuery
            .OrderByDescending(x => x.CreatedOnUtc)
            .ToPagedListAsync(paging.PageNumber, paging.PageSize, cancellationToken);

        var products = result.ToList();
        var items = mapper.Map<List<ProductDto>>(products);

        if (items.Count > 0)
        {
            var categories = await session.Query<CategoryEntity>()
            .ToListAsync(cancellationToken);
            var brands = await session.Query<BrandEntity>()
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                var product = result.FirstOrDefault(p => p.Id == item.Id);

                if (product == null) continue;

                if (product.CategoryIds != null && product.CategoryIds.Count > 0)
                {
                    foreach (var categoryId in product.CategoryIds)
                    {
                        var category = categories.FirstOrDefault(c => c.Id == categoryId);
                        if (category != null)
                        {
                            item.CategoryNames ??= [];
                            item.CategoryNames.Add(category.Name!);
                            item.CategoryIds ??= [];
                            item.CategoryIds.Add(category.Id);
                        }
                    }
                }

                if (product.BrandId.HasValue)
                {
                    var brand = brands.FirstOrDefault(b => b.Id == product.BrandId.Value);
                    if (brand != null)
                    {
                        item.BrandName = brand.Name;
                        item.BrandId = brand.Id;
                    }
                }
            }
        }

        // Names where the search text matches earliest come first on the page.
        if (relevance != null)
        {
            items = items
                .OrderBy(i => relevance.Match(i.Name ?? string.Empty) is { Success: true } m ? m.Index : int.MaxValue)
                .ToList();
        }

        var reponse = new GetProductsResult(items, totalCount, paging);

        return reponse;
    }

    #endregion
}