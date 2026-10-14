#region using

using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Models.Filters;
using Catalog.Domain.Entities;
using Common.Models;
using Marten;
using Marten.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class GetProductsQuerySearchTextTests
{
    #region Fields, Properties and Indexers

    public static TheoryData<string> SpecialCharacterSearches => new()
    {
        // The value seen in Prod: "Invalid pattern 'c++' at offset 3. Nested quantifier '+'."
        "c++",
        "(",
        "[abc",
        "*",
        "?",
        "\\"
    };

    #endregion

    #region Implementations

    [Theory]
    [MemberData(nameof(SpecialCharacterSearches))]
    public async Task Handle_DoesNotThrowRegexParseException_ForSpecialCharacterSearchText(string searchText)
    {
        // STUB: the Marten document session is replaced at the storage boundary. The handler's
        // first query-building call on the product queryable throws StorageReachedException, so
        // reaching storage means the search text was accepted before any query ran.
        var session = DispatchProxy.Create<IDocumentSession, SessionStub>();
        var handler = new GetProductsQueryHandler(session, mapper: null!);
        var query = new GetProductsQuery(new GetProductsFilter(searchText, null), new PaginationRequest());

        var exception = await Record.ExceptionAsync(() => handler.Handle(query, CancellationToken.None));

        Assert.IsNotType<RegexParseException>(exception);
        Assert.IsType<StorageReachedException>(exception);
    }

    #endregion

    #region Stubs

    public sealed class StorageReachedException : Exception;

    public class SessionStub : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod is { Name: "Query", IsGenericMethod: true } && (args == null || args.Length == 0))
            {
                var entityType = targetMethod.GetGenericArguments()[0];
                Assert.Equal(typeof(ProductEntity), entityType);

                return DispatchProxy.Create(
                    typeof(IMartenQueryable<>).MakeGenericType(entityType),
                    typeof(QueryableStub));
            }

            throw new StorageReachedException();
        }
    }

    public class QueryableStub : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new StorageReachedException();
        }
    }

    #endregion
}
