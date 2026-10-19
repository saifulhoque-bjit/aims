namespace Catalog.Application.Dtos.Products;

public class PublishProductDto : ProductInfoDto
{
    #region Fields, Properties and Indexers

    public PublishProductImageDto? Thumbnail { get; set; }

    public string? ThumbnailUrl { get; set; }

    public List<PublishProductImageDto>? Images { get; set; }

    /// <summary>
    /// Each category of the product as a breadcrumb, from the top-level category down,
    /// for example "Electronics / Laptops".
    /// </summary>
    public List<string>? CategoryPaths { get; set; }

    #endregion
}
