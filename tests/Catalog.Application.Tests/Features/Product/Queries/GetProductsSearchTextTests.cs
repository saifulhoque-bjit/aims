#region using

using AutoMapper;
using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Models.Filters;
using Catalog.Domain.Entities;
using Common.Models;
using Marten;
using Marten.Linq;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class GetProductsSearchTextTests
{
    #region Implementations

    // GET /admin/products?searchText=c%2B%2B (and "(", "[", "*") used to fail with
    // RegexParseException before the product query was even built.
    [Theory]
    [InlineData("c++")]
    [InlineData("(")]
    [InlineData("[")]
    [InlineData("*")]
    [InlineData("\\")]
    public async Task Handle_TreatsSearchTextAsPlainText(string searchText)
    {
        Expression? nameFilter = null;
        var provider = Substitute.For<IQueryProvider>();
        var queryable = Substitute.For<IMartenQueryable<ProductEntity>>();
        queryable.Expression.Returns(Expression.Constant(queryable));
        queryable.Provider.Returns(provider);
        provider.CreateQuery<ProductEntity>(Arg.Do<Expression>(e => nameFilter = e)).Returns(queryable);
        var session = Substitute.For<IDocumentSession>();
        session.Query<ProductEntity>().Returns(queryable);
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Paging:MaxPageSize"] = "100" })
            .Build();
        var handler = new GetProductsQueryHandler(session, Substitute.For<IMapper>(), cfg);

        var ex = await Record.ExceptionAsync(() => handler.Handle(
            new GetProductsQuery(new GetProductsFilter(searchText, null), new PaginationRequest(1, 10)),
            CancellationToken.None));

        Assert.False(ex is RegexParseException, ex?.ToString());
        // The handler got past the search step and filtered names by the text as typed.
        Assert.NotNull(nameFilter);
        Assert.Contains("Contains(", nameFilter!.ToString());
    }

    #endregion
}
