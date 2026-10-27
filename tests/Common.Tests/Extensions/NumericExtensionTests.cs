#region using

using System.Globalization;
using Common.Extensions;
using Xunit;

#endregion

namespace Common.Tests.Extensions;

// SPEC: spec-a7ad8e
public class NumericExtensionTests
{
    #region Fields, Properties and Indexers

    public static TheoryData<int, string> ShortNumberCases => new()
    {
        // Below 1000 - returned unchanged, negatives included.
        { 0, "0" },
        { 999, "999" },
        { -1500, "-1500" },
        // 1000 to 999999 - the behaviour that already worked.
        { 1000, "1k" },
        { 1050, "1k" },
        { 1099, "1k" },
        { 1100, "1.1k" },
        { 1500, "1.5k" },
        { 999999, "999.9k" },
        // A million or more used to be cut at the first comma group ("1.2k").
        { 1000000, "1000k" },
        { 1234567, "1234.5k" },
        { 2500000, "2500k" },
        { int.MaxValue, "2147483.6k" }
    };

    public static TheoryData<long, string> FileSizeCases => new()
    {
        // Below 1024 - stays in Bytes, as a whole number.
        { 0, "0 Bytes" },
        { 1, "1 Bytes" },
        { 512, "512 Bytes" },
        { 513, "513 Bytes" },
        { 600, "600 Bytes" },
        { 1023, "1023 Bytes" },
        { -600, "-600 Bytes" },
        // Moves to the next unit at exactly 1024, not at half a unit.
        { 1024, "1.0 KB" },
        { 1536, "1.5 KB" },
        { 524289, "512.0 KB" },
        { 1048575, "1,024.0 KB" },
        { 1048576, "1.0 MB" },
        { 536870913, "512.0 MB" },
        { 1073741824, "1.0 GB" },
        // Stops at PB instead of running past the last suffix.
        { 1125899906842624L * 600, "600.0 PB" },
        { long.MaxValue, "8,192.0 PB" }
    };

    #endregion

    #region Implementations

    [Theory]
    [MemberData(nameof(ShortNumberCases))]
    public void ToShortNumber_AbbreviatesThousandsWithK(int value, string expected)
    {
        var result = value.ToShortNumber();

        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(ShortNumberCases))]
    public void ToShortNumber_IsTheSame_UnderACultureWithDotGrouping(int value, string expected)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var result = value.ToShortNumber();

            Assert.Equal(expected, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(FileSizeCases))]
    public void ToFileSize_StaysInAUnitBelow1024AndStopsAtPB(long value, string expected)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var result = value.ToFileSize();

            Assert.Equal(expected, result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    #endregion
}
