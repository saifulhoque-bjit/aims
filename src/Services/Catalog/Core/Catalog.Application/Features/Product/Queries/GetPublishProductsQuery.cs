#region using

using AutoMapper;
using BuildingBlocks.Pagination.Extensions;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Models.Filters;
using Catalog.Application.Models.Results;
using Catalog.Domain.Entities;
using Marten;
using Marten.Pagination;

#endregion

namespace Catalog.Application.Features.Product.Queries;

public sealed record GetPublishProductsQuery(
    GetPublishProductsFilter Filter,
    PaginationRequest Paging) : IQuery<GetPublishProductsResult>;

public sealed class GetPublishProductsQueryHandler(IDocumentSession session, IMapper mapper)
    : IQueryHandler<GetPublishProductsQuery, GetPublishProductsResult>
{
    #region Implementations

    public async Task<GetPublishProductsResult> Handle(GetPublishProductsQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Filter;
        // SPEC: spec-61ecda
        var paging = query.Paging.Normalized();
        var productQuery = session.Query<ProductEntity>().Where(x => x.Published);

        if (!filter.SearchText.IsNullOrWhiteSpace())
        {
            var search = filter.SearchText.Trim();
            productQuery = productQuery.Where(x => x.Name != null && x.Name.Contains(search));
        }

        var totalCount = await productQuery.CountAsync(cancellationToken);
        var result = await productQuery
            .OrderByDescending(x => x.CreatedOnUtc)
            .ToPagedListAsync(paging.PageNumber, paging.PageSize, cancellationToken);

        var products = result.ToList();
        var items = mapper.Map<List<PublishProductDto>>(products);

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

                item.ThumbnailUrl = product.Thumbnail!.PublicURL;

                if (product.CategoryIds != null && product.CategoryIds.Count > 0)
                {
                    foreach (var categoryId in product.CategoryIds)
                    {
                        var category = categories.FirstOrDefault(c => c.Id == categoryId);
                        if (category != null)
                        {
                            item.CategoryNames ??= [];
                            item.CategoryNames.Add(category.Name!);
                        }
                    }
                }

                if (product.BrandId.HasValue)
                {
                    item.BrandName = brands.First(b => b.Id == product.BrandId.Value).Name;
                }
            }
        }

        var reponse = new GetPublishProductsResult(items, totalCount, paging);

        return reponse;
    }

    #endregion
}