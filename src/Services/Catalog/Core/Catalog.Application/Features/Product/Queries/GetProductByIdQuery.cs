#region using

using AutoMapper;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Models.Results;
using Catalog.Domain.Entities;
using Marten;
using Microsoft.Extensions.Logging;

#endregion

namespace Catalog.Application.Features.Product.Queries;

public sealed record GetProductByIdQuery(Guid ProductId) : IQuery<GetProductByIdResult>;

public sealed class GetProductByIdQueryHandler(IDocumentSession session, IMapper mapper, ILogger<GetProductByIdQueryHandler> logger)
    : IQueryHandler<GetProductByIdQuery, GetProductByIdResult>
{
    #region Implementations

    public async Task<GetProductByIdResult> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var result = await session.LoadAsync<ProductEntity>(query.ProductId)
            ?? throw new NotFoundException(MessageCode.ResourceNotFound, query.ProductId);

        var categories = await session.Query<CategoryEntity>()
            .ToListAsync(cancellationToken);
        var brands = await session.Query<BrandEntity>()
            .ToListAsync(cancellationToken);

        var reponse = mapper.Map<ProductDto>(result);

        if (result.CategoryIds != null && result.CategoryIds.Count > 0)
        {
            foreach (var categoryId in result.CategoryIds)
            {
                var category = categories.FirstOrDefault(c => c.Id == categoryId);
                if (category != null)
                {
                    reponse.CategoryNames ??= [];
                    reponse.CategoryNames.Add(category.Name!);
                    reponse.CategoryIds ??= [];
                    reponse.CategoryIds.Add(category.Id);
                }
            }
        }

        if (result.BrandId.HasValue)
        {
            var brand = brands.FirstOrDefault(b => b.Id == result.BrandId.Value);
            if (brand != null)
            {
                reponse.BrandName = brand.Name;
                reponse.BrandId = brand.Id;
            }
        }

        // AMS incident 119 (spec-0be21a, lifecycle: agreed) — the deliberately seeded
        // NullReferenceException here has been remediated under that ruling. An
        // unpublished product has no moderator review note yet; that is now detected
        // and reported explicitly instead of being dereferenced, so the detail view
        // returns 200 rather than crashing the request.
        if (!result.Published)
        {
            logger.LogInformation(
                "Unpublished product detail requested; moderator review note is not yet available. ProductId: {ProductId}",
                query.ProductId);
        }

        return new GetProductByIdResult(reponse);
    }

    #endregion
}