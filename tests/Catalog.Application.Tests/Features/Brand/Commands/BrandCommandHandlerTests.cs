#region using

using BuildingBlocks.Exceptions;
using Catalog.Application.Dtos.Brands;
using Catalog.Application.Features.Brand.Commands;
using Catalog.Application.Repositories;
using Catalog.Domain.Entities;
using Common.Constants;
using Common.ValueObjects;
using Marten;
using NSubstitute;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Brand.Commands;

public class BrandCommandHandlerTests
{
    #region Fields, Properties and Indexers

    private static readonly Actor TestActor = Actor.User("admin");

    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();

    private readonly ISlugRepository _slugRepository = Substitute.For<ISlugRepository>();

    #endregion

    #region Implementations

    [Fact]
    public async Task CreateBrand_Throws_WhenAnotherBrandHasTheSameSlug()
    {
        _slugRepository.BrandSlugExistsAsync("Acme", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateBrandCommandHandler(_session, _slugRepository);

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new CreateBrandCommand(new CreateBrandDto { Name = "Acme" }, TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.BrandSlugAlreadyExists, ex.Message);
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateBrand_Saves_WhenNoOtherBrandHasTheSlug()
    {
        var handler = new CreateBrandCommandHandler(_session, _slugRepository);

        var id = await handler.Handle(new CreateBrandCommand(new CreateBrandDto { Name = "Acme" }, TestActor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _session.Received(1).Store(Arg.Is<BrandEntity>(x => x.Id == id && x.Slug == "Acme"));
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBrand_Throws_WhenAnotherBrandHasTheSameSlug()
    {
        var brand = BrandEntity.Create(Guid.NewGuid(), "Globex", "Globex", "admin");
        _session.LoadAsync<BrandEntity>(brand.Id, Arg.Any<CancellationToken>()).Returns(brand);
        _slugRepository.BrandSlugExistsAsync("Acme", brand.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateBrandCommandHandler(_session, _slugRepository);

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new UpdateBrandCommand(brand.Id, new UpdateBrandDto { Name = "Acme" }, TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.BrandSlugAlreadyExists, ex.Message);
        Assert.Equal("Globex", brand.Name);
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBrand_Saves_WhenTheBrandKeepsItsOwnName()
    {
        var brand = BrandEntity.Create(Guid.NewGuid(), "Acme", "Acme", "admin");
        _session.LoadAsync<BrandEntity>(brand.Id, Arg.Any<CancellationToken>()).Returns(brand);
        // Without excluding the brand itself, its own slug would count as taken.
        _slugRepository.BrandSlugExistsAsync("Acme", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateBrandCommandHandler(_session, _slugRepository);

        var id = await handler.Handle(new UpdateBrandCommand(brand.Id, new UpdateBrandDto { Name = "Acme" }, TestActor), CancellationToken.None);

        Assert.Equal(brand.Id, id);
        await _slugRepository.Received(1).BrandSlugExistsAsync("Acme", brand.Id, Arg.Any<CancellationToken>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
