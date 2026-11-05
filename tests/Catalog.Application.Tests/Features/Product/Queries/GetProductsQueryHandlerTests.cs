#region using

using AutoMapper;
using Catalog.Application.Features.Product.Queries;
using Catalog.Application.Models.Filters;
using Common.Models;
using Marten;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using System.Text.RegularExpressions;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class GetProductsQueryHandlerTests
{
    #region Implementations

    [Theory]
    [InlineData("c++")]
    [InlineData("C#")]
    [InlineData("(")]
    [InlineData("[usb")]
    public async Task Handle_DoesNotParseSearchTextAsRegex(string searchText)
    {
        // Stubs: IDocumentSession and IMapper are substitutes; the Marten query
        // after the search filter cannot run without a database, so any exception
        // from that boundary is accepted. Only a regex parse failure is the defect.
        var session = Substitute.For<IDocumentSession>();
        var mapper = Substitute.For<IMapper>();
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Paging:MaxPageSize"] = "100" })
            .Build();
        var handler = new GetProductsQueryHandler(session, mapper, cfg);
        var query = new GetProductsQuery(new GetProductsFilter(searchText, null), new PaginationRequest(1, 10));

        var exception = await Record.ExceptionAsync(() => handler.Handle(query, CancellationToken.None));

        Assert.False(exception is RegexParseException, exception?.Message);
    }

    #endregion
}
