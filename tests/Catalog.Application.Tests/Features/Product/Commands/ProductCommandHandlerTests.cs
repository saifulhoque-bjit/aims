#region using

using AutoMapper;
using BuildingBlocks.Exceptions;
using Catalog.Application.Dtos.Products;
using Catalog.Application.Features.Product.Commands;
using Catalog.Application.Repositories;
using Catalog.Application.Services;
using Catalog.Domain.Entities;
using Common.Constants;
using Common.Models;
using Common.ValueObjects;
using Marten;
using MediatR;
using NSubstitute;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Commands;

public class ProductCommandHandlerTests
{
    #region Fields, Properties and Indexers

    private static readonly Actor TestActor = Actor.User("admin");

    private readonly IMapper _mapper = Substitute.For<IMapper>();

    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();

    private readonly IMinIOCloudService _minIO = Substitute.For<IMinIOCloudService>();

    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private readonly ISender _sender = Substitute.For<ISender>();

    private readonly ISlugRepository _slugRepository = Substitute.For<ISlugRepository>();

    #endregion

    #region Ctors

    public ProductCommandHandlerTests()
    {
        _minIO.UploadFilesAsync(Arg.Any<List<UploadFileBytes>>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<UploadFileResult>());
    }

    #endregion

    #region Implementations

    [Fact]
    public async Task CreateProduct_Throws_WhenAnotherProductHasTheSameSlug()
    {
        _slugRepository.ProductSlugExistsAsync("Phone-X", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new CreateProductCommand(CreateDto("Phone X"), TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.ProductSlugAlreadyExists, ex.Message);
        // Rejected before any image is uploaded, so no orphan files are left behind.
        await _minIO.DidNotReceive().UploadFilesAsync(Arg.Any<List<UploadFileBytes>>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateProduct_Saves_WhenNoOtherProductHasTheSlug()
    {
        var handler = CreateHandler();

        var id = await handler.Handle(new CreateProductCommand(CreateDto("Phone X"), TestActor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _session.Received(1).Store(Arg.Is<ProductEntity>(x => x.Id == id && x.Slug == "Phone-X"));
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_Throws_WhenAnotherProductHasTheSameSlug()
    {
        var product = CreateProduct("Tablet Y", "Tablet-Y");
        _session.LoadAsync<ProductEntity>(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        _slugRepository.ProductSlugExistsAsync("Phone-X", product.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = UpdateHandler();

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new UpdateProductCommand(product.Id, UpdateDto("Phone X"), TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.ProductSlugAlreadyExists, ex.Message);
        Assert.Equal("Tablet Y", product.Name);
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_Saves_WhenTheProductKeepsItsOwnName()
    {
        var product = CreateProduct("Phone X", "Phone-X");
        _session.LoadAsync<ProductEntity>(product.Id, Arg.Any<CancellationToken>()).Returns(product);
        // Without excluding the product itself, its own slug would count as taken.
        _slugRepository.ProductSlugExistsAsync("Phone-X", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = UpdateHandler();

        var id = await handler.Handle(new UpdateProductCommand(product.Id, UpdateDto("Phone X"), TestActor), CancellationToken.None);

        Assert.Equal(product.Id, id);
        await _slugRepository.Received(1).ProductSlugExistsAsync("Phone-X", product.Id, Arg.Any<CancellationToken>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region Methods

    private CreateProductCommandHandler CreateHandler()
        => new(_mapper, _session, _minIO, _sender, _slugRepository);

    private UpdateProductCommandHandler UpdateHandler()
        => new(_mapper, _session, _minIO, _mediator, _sender, _slugRepository);

    private static ProductEntity CreateProduct(string name, string slug)
        => ProductEntity.Create(Guid.NewGuid(), name, "SKU-1", "Short", "Long", slug, 100m, null, null, null, "admin");

    private static CreateProductDto CreateDto(string name) => new()
    {
        Name = name,
        Sku = "SKU-1",
        ShortDescription = "Short",
        LongDescription = "Long",
        Price = 100m,
        SEOTitle = name,
        UploadThumbnail = new UploadFileBytes { FileName = "thumb.jpg", ContentType = "image/jpeg", Bytes = [1] }
    };

    private static UpdateProductDto UpdateDto(string name) => new()
    {
        Name = name,
        Sku = "SKU-1",
        ShortDescription = "Short",
        LongDescription = "Long",
        Price = 100m
    };

    #endregion
}
