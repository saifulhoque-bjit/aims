#region using

using Catalog.Domain.Entities;

#endregion

namespace Catalog.Application.Features.Category;

/// <summary>
///     Walks the parent chain of the category tree.
/// </summary>
public static class CategoryHierarchy
{
    #region Constants

    // The category tree is shallow; a deeper chain means the data is wrong.
    private const int MaxCategoryDepth = 8;

    #endregion

    #region Methods

    /// <summary>
    ///     Returns the category's path from its root, e.g. "Electronics / Phones".
    /// </summary>
    public static string BuildPath(
        CategoryEntity category,
        IReadOnlyDictionary<Guid, CategoryEntity> categoriesById)
    {
        var names = new List<string> { category.Name! };
        var visited = new HashSet<Guid> { category.Id };
        var current = category;
        var depth = 0;

        while (current.ParentId is { } parentId && categoriesById.TryGetValue(parentId, out var parent))
        {
            // Parents that loop back (A -> B -> A) end the path at the first repeat.
            if (!visited.Add(parent.Id))
            {
                break;
            }

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

    /// <summary>
    ///     Whether giving <paramref name="categoryId" /> the parent <paramref name="parentId" />
    ///     would put the category under itself, i.e. the parent is the category or one of its descendants.
    /// </summary>
    public static bool WouldCreateCycle(
        Guid categoryId,
        Guid parentId,
        IReadOnlyDictionary<Guid, Guid?> parentIdsById)
    {
        var visited = new HashSet<Guid>();
        Guid? current = parentId;

        while (current is { } id && visited.Add(id))
        {
            if (id == categoryId)
            {
                return true;
            }

            current = parentIdsById.TryGetValue(id, out var next) ? next : null;
        }

        return false;
    }

    #endregion
}
