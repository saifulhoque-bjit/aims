#region using

using Catalog.Application.Features.Product.Queries;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class ProductSearchRankingTests
{
    #region Implementations

    [Theory]
    [InlineData("Learn C++ Fast", "c++", 6)]
    [InlineData("Cable (USB-C)", "(", 6)]
    [InlineData("Array [2]", "[", 6)]
    [InlineData("Star * Lamp", "*", 5)]
    [InlineData("Path\\To", "\\", 4)]
    [InlineData("Why?", "?", 3)]
    public void MatchPosition_MatchesRegexSymbolsLiterally(string name, string searchText, int expected)
    {
        Assert.Equal(expected, ProductSearchRanking.MatchPosition(name, searchText));
    }

    [Fact]
    public void MatchPosition_DotMatchesOnlyALiteralDot()
    {
        Assert.Equal(int.MaxValue, ProductSearchRanking.MatchPosition("Version 1x5", "1.5"));
        Assert.Equal(8, ProductSearchRanking.MatchPosition("Version 1.5", "1.5"));
    }

    [Fact]
    public void MatchPosition_ReturnsMaxValue_WhenNameIsMissingOrHasNoMatch()
    {
        Assert.Equal(int.MaxValue, ProductSearchRanking.MatchPosition(null, "c++"));
        Assert.Equal(int.MaxValue, ProductSearchRanking.MatchPosition("Java Book", "c++"));
    }

    [Fact]
    public void OrderingByMatchPosition_PutsEarliestCaseInsensitiveMatchFirst_AndNonMatchesLast()
    {
        var names = new[] { "Java Book", "Learn C++", "c++ primer", "The C++ Way" };

        var ordered = names.OrderBy(n => ProductSearchRanking.MatchPosition(n, "C++")).ToList();

        Assert.Equal(new[] { "c++ primer", "The C++ Way", "Learn C++", "Java Book" }, ordered);
    }

    #endregion
}
