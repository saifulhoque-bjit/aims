#region using

#endregion

namespace Catalog.Application.Features.Product.Queries;

// SPEC: spec-87f298
/// <summary>
///     Ranks admin product search results by where the search text first appears in the
///     product name. The search text is plain text: regex symbols such as "+", "(", "[",
///     "*" or "." are matched literally and never parsed as a pattern.
/// </summary>
public static class ProductSearchRanking
{
    #region Fields, Properties and Indexers

    /// <summary>
    ///     Returns the earliest case-insensitive position of <paramref name="searchText" /> in
    ///     <paramref name="name" />, or <see cref="int.MaxValue" /> when the name does not contain it.
    /// </summary>
    public static int MatchPosition(string? name, string? searchText)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(searchText))
        {
            return int.MaxValue;
        }

        var index = name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

        return index < 0 ? int.MaxValue : index;
    }

    #endregion
}
