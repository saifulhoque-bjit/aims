#region using

using System.Globalization;

#endregion

namespace Common.Extensions;

public static class NumericExtension
{
    #region Fields, Properties and Indexers

    private static readonly string[] FileSizeSuffixes = ["Bytes", "KB", "MB", "GB", "TB", "PB"];

    #endregion

    #region Methods

    public static string ToFileSize(this long value)
    {
        int counter = 0;
        decimal number = value;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {FileSizeSuffixes[counter]}";
    }

    // SPEC: spec-a7ad8e
    public static string ToShortNumber(this int value)
    {
        const string baseString = "k";
        if (value < 1000)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        // Arithmetic, not a culture-formatted string: splitting "1,234,567" at its
        // group separators cut a million or more down to its first group.
        int thousands = value / 1000;
        int tenths = value % 1000 / 100;

        return tenths > 0
            ? string.Create(CultureInfo.InvariantCulture, $"{thousands}.{tenths}{baseString}")
            : string.Create(CultureInfo.InvariantCulture, $"{thousands}{baseString}");
    }

    public static TEnum? ToEnum<TEnum>(this int value) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value)
            ? (TEnum)Enum.ToObject(typeof(TEnum), value)
            : null;
    }

    #endregion
}
