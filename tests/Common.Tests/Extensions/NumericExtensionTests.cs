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

    #endregion
}
