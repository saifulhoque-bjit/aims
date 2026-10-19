#region using

using Catalog.Domain.Entities;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Commands;

public class ReplaceThumbnailTests
{
    #region Fields, Properties and Indexers

    private const string ExistingUrl = "https://cdn.example.com/products/gucci-handbag.jpg";

    #endregion

    #region Implementations

    [Fact]
    public void ReplaceThumbnail_KeepsThumbnail_WhenEditSendsNoUploadAndNoCurrentUrl()
    {
        // The admin edit form sends neither a thumbnail file nor the current thumbnail URL
        // when only the price changes. That edit used to clear the thumbnail and break the
        // storefront product list.
        var product = CreateProductWithThumbnail();

        product.ReplaceThumbnail(null);

        Assert.NotNull(product.Thumbnail);
        Assert.Equal(ExistingUrl, product.Thumbnail!.PublicURL);
    }

    [Fact]
    public void ReplaceThumbnail_ReplacesThumbnail_WhenEditUploadsNewFile()
    {
        var product = CreateProductWithThumbnail();
        var uploaded = new ProductImageEntity { FileId = "new", PublicURL = "https://cdn.example.com/products/new.jpg" };

        product.ReplaceThumbnail(uploaded);

        Assert.Same(uploaded, product.Thumbnail);
    }

    private static ProductEntity CreateProductWithThumbnail()
    {
        var product = ProductEntity.Create(
            Guid.NewGuid(), "Gucci Leather Handbag", "GUCCI-01", "short", "long", "gucci-leather-handbag",
            1200m, null, null, null, "tester");
        product.AddOrUpdateThumbnail(new ProductImageEntity { FileId = "old", PublicURL = ExistingUrl });
        return product;
    }

    #endregion
}
