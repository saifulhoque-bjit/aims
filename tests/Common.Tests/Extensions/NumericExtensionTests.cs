#region using

using Common.Extensions;
using Xunit;

#endregion

namespace Common.Tests.Extensions;

public class NumericExtensionTests
{
    #region Implementations

    [Theory]
    [InlineData(999, "999")]
    [InlineData(1000, "1k")]
    [InlineData(1050, "1k")]
    [InlineData(1234, "1.2k")]
    [InlineData(999999, "999.9k")]
    [InlineData(1000000, "1000k")]
    [InlineData(1234567, "1234.5k")]
    public void ToShortNumber_AbbreviatesAsThousands(int value, string expected)
    {
        Assert.Equal(expected, value.ToShortNumber());
    }

    #endregion
}
