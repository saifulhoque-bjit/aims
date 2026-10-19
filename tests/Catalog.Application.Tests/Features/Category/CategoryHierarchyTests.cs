#region using

using Catalog.Application.Features.Category;
using Catalog.Domain.Entities;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Category;

public class CategoryHierarchyTests
{
    #region Fields, Properties and Indexers

    // The seeded ids of Electronics and its children Phones and Laptops.
    private static readonly Guid ElectronicsId = Guid.Parse("a2d8c5a8-2a64-4b6d-a1c0-0c8b4b9c1a11");
    private static readonly Guid PhonesId = Guid.Parse("b61c0f19-8d1c-4a2e-8f9a-3b5a85d17e12");
    private static readonly Guid LaptopsId = Guid.Parse("6f6b1e0b-65b0-4c42-9a8d-2a5b3e7c8f13");

    #endregion

    #region Implementations

    [Fact]
    public void BuildPath_ReturnsPathFromRoot()
    {
        var categories = SeedTree();

        var path = CategoryHierarchy.BuildPath(categories[PhonesId], categories);

        Assert.Equal("Electronics / Phones", path);
    }

    [Fact]
    public void BuildPath_DoesNotThrow_WhenParentsFormALoop()
    {
        // Electronics re-parented under its own child Phones: Phones -> Electronics -> Phones -> ...
        var categories = SeedTree();
        categories[ElectronicsId].ParentId = PhonesId;

        var phonesPath = CategoryHierarchy.BuildPath(categories[PhonesId], categories);
        var laptopsPath = CategoryHierarchy.BuildPath(categories[LaptopsId], categories);

        Assert.Equal("Electronics / Phones", phonesPath);
        Assert.Equal("Phones / Electronics / Laptops", laptopsPath);
    }

    [Theory]
    [InlineData("phones")]
    [InlineData("laptops")]
    public void WouldCreateCycle_IsTrue_WhenNewParentIsADescendant(string child)
    {
        var parentIds = SeedTree().ToDictionary(x => x.Key, x => x.Value.ParentId);
        var childId = child == "phones" ? PhonesId : LaptopsId;

        Assert.True(CategoryHierarchy.WouldCreateCycle(ElectronicsId, childId, parentIds));
    }

    [Fact]
    public void WouldCreateCycle_IsFalse_ForAValidMove()
    {
        var parentIds = SeedTree().ToDictionary(x => x.Key, x => x.Value.ParentId);

        // Phones moved under Laptops, its sibling.
        Assert.False(CategoryHierarchy.WouldCreateCycle(PhonesId, LaptopsId, parentIds));
    }

    [Fact]
    public void WouldCreateCycle_Terminates_WhenStoredParentsAlreadyLoop()
    {
        var categories = SeedTree();
        categories[ElectronicsId].ParentId = PhonesId;
        var parentIds = categories.ToDictionary(x => x.Key, x => x.Value.ParentId);
        var otherId = Guid.NewGuid();
        parentIds[otherId] = null;

        Assert.False(CategoryHierarchy.WouldCreateCycle(otherId, PhonesId, parentIds));
    }

    #endregion

    #region Methods

    private static Dictionary<Guid, CategoryEntity> SeedTree()
    {
        return new[]
        {
            CategoryEntity.Create(ElectronicsId, "Electronics", "", "electronics", "test"),
            CategoryEntity.Create(PhonesId, "Phones", "", "phones", "test", ElectronicsId),
            CategoryEntity.Create(LaptopsId, "Laptops", "", "laptops", "test", ElectronicsId)
        }.ToDictionary(c => c.Id);
    }

    #endregion
}
