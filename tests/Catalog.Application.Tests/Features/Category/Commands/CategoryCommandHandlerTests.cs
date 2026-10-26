#region using

using BuildingBlocks.Exceptions;
using Catalog.Application.Dtos.Categories;
using Catalog.Application.Features.Category.Commands;
using Catalog.Application.Repositories;
using Catalog.Domain.Entities;
using Common.Constants;
using Common.ValueObjects;
using Marten;
using NSubstitute;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Category.Commands;

public class CategoryCommandHandlerTests
{
    #region Fields, Properties and Indexers

    private static readonly Actor TestActor = Actor.User("admin");

    private readonly IDocumentSession _session = Substitute.For<IDocumentSession>();

    private readonly ISlugRepository _slugRepository = Substitute.For<ISlugRepository>();

    #endregion

    #region Implementations

    [Fact]
    public async Task CreateCategory_Throws_WhenAnotherCategoryHasTheSameSlug()
    {
        _slugRepository.CategorySlugExistsAsync("Laptops", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateCategoryCommandHandler(_session, _slugRepository);

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new CreateCategoryCommand(new CreateCategoryDto { Name = "Laptops" }, TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.CategorySlugAlreadyExists, ex.Message);
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateCategory_Saves_WhenNoOtherCategoryHasTheSlug()
    {
        var handler = new CreateCategoryCommandHandler(_session, _slugRepository);

        var id = await handler.Handle(new CreateCategoryCommand(new CreateCategoryDto { Name = "Laptops" }, TestActor), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, id);
        _session.Received(1).Store(Arg.Is<CategoryEntity>(x => x.Id == id && x.Slug == "Laptops"));
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_Throws_WhenAnotherCategoryHasTheSameSlug()
    {
        var category = CategoryEntity.Create(Guid.NewGuid(), "Phones", string.Empty, "Phones", "admin");
        _session.LoadAsync<CategoryEntity>(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _slugRepository.CategorySlugExistsAsync("Laptops", category.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateCategoryCommandHandler(_session, _slugRepository);

        var ex = await Assert.ThrowsAsync<ClientValidationException>(() =>
            handler.Handle(new UpdateCategoryCommand(category.Id, new UpdateCategoryDto { Name = "Laptops" }, TestActor), CancellationToken.None));

        Assert.Equal(MessageCode.CategorySlugAlreadyExists, ex.Message);
        Assert.Equal("Phones", category.Name);
        await _session.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_Saves_WhenTheCategoryKeepsItsOwnName()
    {
        var category = CategoryEntity.Create(Guid.NewGuid(), "Laptops", string.Empty, "Laptops", "admin");
        _session.LoadAsync<CategoryEntity>(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        // Without excluding the category itself, its own slug would count as taken.
        _slugRepository.CategorySlugExistsAsync("Laptops", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new UpdateCategoryCommandHandler(_session, _slugRepository);

        var id = await handler.Handle(new UpdateCategoryCommand(category.Id, new UpdateCategoryDto { Name = "Laptops", Description = "Portable computers" }, TestActor), CancellationToken.None);

        Assert.Equal(category.Id, id);
        Assert.Equal("Portable computers", category.Description);
        await _slugRepository.Received(1).CategorySlugExistsAsync("Laptops", category.Id, Arg.Any<CancellationToken>());
        await _session.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}
