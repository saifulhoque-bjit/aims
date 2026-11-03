#region using

using System.Globalization;
using Common.Extensions;
using Xunit;

#endregion

namespace Common.Tests.Extensions;

// SPEC: spec-4a79cd
public class NumericExtensionTests
{
    #region Implementations

    [Theory]
    [InlineData(0L, "0 Bytes")]
    [InlineData(512L, "512 Bytes")]
    // 513 to 1023 used to round up into KB ("0.6 KB" for 600).
    [InlineData(513L, "513 Bytes")]
    [InlineData(600L, "600 Bytes")]
    [InlineData(1023L, "1023 Bytes")]
    public void ToFileSize_StaysInBytes_BelowOneKilobyte(long value, string expected)
    {
        Assert.Equal(expected, FormatInvariant(value));
    }

    [Theory]
    [InlineData(1024L, "1.0 KB")]
    [InlineData(1536L, "1.5 KB")]
    // Just under 1 MB stays in KB; the shown value is only rounded for display.
    [InlineData(1048575L, "1,024.0 KB")]
    [InlineData(1048576L, "1.0 MB")]
    public void ToFileSize_MovesUpAUnit_OnlyAtTenTwentyFourOfTheCurrentUnit(long value, string expected)
    {
        Assert.Equal(expected, FormatInvariant(value));
    }

    [Fact]
    public void ToFileSize_StaysInPetabytes_ForTheLargestValues()
    {
        // long.MaxValue is about 8 EB: there is no suffix past PB, so it stays in PB
        // instead of indexing past the end of the suffix list.
        Assert.Equal("8,192.0 PB", FormatInvariant(long.MaxValue));
    }

    private static string FormatInvariant(long value)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            return value.ToFileSize();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    #endregion
}
