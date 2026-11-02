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

    public static string ToShortNumber(this int value)
    {
        const string baseString = "k";
        if (value < 1000)
        {
            return value.ToString();
        }

        // Arithmetic, not split at group separators: "1,234,567" kept only its
        // first two groups and read as thousands ("1.2k").
        int thousands = value / 1000;
        int tenths = value % 1000 / 100;

        return tenths > 0
            ? $"{thousands}.{tenths}{baseString}"
            : thousands + baseString;
    }

    public static TEnum? ToEnum<TEnum>(this int value) where TEnum : struct, Enum
    {
        return Enum.IsDefined(typeof(TEnum), value)
            ? (TEnum)Enum.ToObject(typeof(TEnum), value)
            : null;
    }

    #endregion
}
