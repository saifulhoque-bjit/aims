#region using

using AutoMapper;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Models.Results;
using Catalog.Domain.Entities;
using Marten;

#endregion

namespace Catalog.Application.Features.Product.Queries;

public sealed record GetPublishProductByIdQuery(Guid ProductId) : IQuery<GetPublishProductByIdResult>;

public sealed class GetPublishProductByIdQueryHandler(IDocumentSession session, IMapper mapper)
    : IQueryHandler<GetPublishProductByIdQuery, GetPublishProductByIdResult>
{
    #region Constants

    // The category tree is shallow; a deeper chain means the data is wrong.
    private const int MaxCategoryDepth = 8;

    #endregion

    #region Implementations

    public async Task<GetPublishProductByIdResult> Handle(GetPublishProductByIdQuery query, CancellationToken cancellationToken)
    {
        var result = await session.Query<ProductEntity>()
            .Where(x => x.Id == query.ProductId && x.Published)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(MessageCode.ResourceNotFound, query.ProductId);

        var categories = await session.Query<CategoryEntity>()
            .ToListAsync(cancellationToken);
        var brands = await session.Query<BrandEntity>()
            .ToListAsync(cancellationToken);

        var reponse = mapper.Map<PublishProductDto>(result);
        reponse.ThumbnailUrl = result.Thumbnail?.PublicURL;

        if (result.CategoryIds != null && result.CategoryIds.Count > 0)
        {
            var categoriesById = categories.ToDictionary(c => c.Id);

            foreach (var categoryId in result.CategoryIds)
            {
                var category = categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    reponse.CategoryNames ??= [];
                    reponse.CategoryNames.Add(category.Name!);
                    reponse.CategoryPaths ??= [];
                    reponse.CategoryPaths.Add(BuildCategoryPath(category, categoriesById));
                }
            }
        }

        if (result.BrandId.HasValue)
        {
            var brand = brands.FirstOrDefault(b => b.Id == result.BrandId.Value);
            if (brand != null)
            {
                reponse.BrandName = brand.Name;
            }
        }

        return new GetPublishProductByIdResult(reponse);
    }

    #endregion

    #region Methods

    private static string BuildCategoryPath(
        CategoryEntity category,
        IReadOnlyDictionary<Guid, CategoryEntity> categoriesById)
    {
        var names = new List<string> { category.Name! };
        var current = category;
        var depth = 0;

        while (current.ParentId is { } parentId && categoriesById.TryGetValue(parentId, out var parent))
        {
            if (++depth > MaxCategoryDepth)
            {
                throw new InvalidOperationException(
                    $"Category {category.Id} is nested deeper than {MaxCategoryDepth} levels.");
            }

            names.Insert(0, parent.Name!);
            current = parent;
        }

        return string.Join(" / ", names);
    }

    #endregion
}