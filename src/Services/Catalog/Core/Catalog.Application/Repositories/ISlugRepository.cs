namespace Catalog.Application.Repositories;

public interface ISlugRepository
{
    #region Methods

    /// <summary>
    /// Returns true when a brand other than <paramref name="excludeId"/> already has <paramref name="slug"/>.
    /// </summary>
    Task<bool> BrandSlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a category other than <paramref name="excludeId"/> already has <paramref name="slug"/>.
    /// </summary>
    Task<bool> CategorySlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a product other than <paramref name="excludeId"/> already has <paramref name="slug"/>.
    /// </summary>
    Task<bool> ProductSlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);

    #endregion
}
