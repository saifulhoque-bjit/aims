#region using

using Common.Configurations;
using Microsoft.Extensions.Configuration;
using Xunit;

#endregion

namespace Catalog.Application.Tests.Features.Product.Queries;

public class ProductListPageSizeTests
{
    #region Implementations

    [Fact]
    public void ShippedSettings_ConfigureMaxPageSize_WhereTheHandlerReadsIt()
    {
        // The service runs without ASPNETCORE_ENVIRONMENT, so only appsettings.json is loaded.
        var cfg = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(Path.Combine("Settings", "appsettings.json"), optional: false)
            .Build();

        var maxPageSize = cfg.GetValue<int>($"{PagingCfg.Section}:{PagingCfg.MaxPageSize}");

        Assert.Equal(100, maxPageSize);
    }

    #endregion
}
