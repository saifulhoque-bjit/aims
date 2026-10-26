#region using

using Catalog.Application.Repositories;
using Catalog.Domain.Entities;
using Marten;

#endregion

namespace Catalog.Infrastructure.Repositories;

public class SlugRepository(IDocumentSession session) : ISlugRepository
{
    #region Implementations

    public async Task<bool> BrandSlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = session.Query<BrandEntity>().Where(x => x.Slug == slug);

        if (excludeId.HasValue)
        {
            var id = excludeId.Value;
            query = query.Where(x => x.Id != id);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> CategorySlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = session.Query<CategoryEntity>().Where(x => x.Slug == slug);

        if (excludeId.HasValue)
        {
            var id = excludeId.Value;
            query = query.Where(x => x.Id != id);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ProductSlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = session.Query<ProductEntity>().Where(x => x.Slug == slug);

        if (excludeId.HasValue)
        {
            var id = excludeId.Value;
            query = query.Where(x => x.Id != id);
        }

        return await query.AnyAsync(cancellationToken);
    }

    #endregion
}
