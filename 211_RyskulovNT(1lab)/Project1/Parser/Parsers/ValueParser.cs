using System.Globalization;
using System.Text.RegularExpressions;

namespace Parser.Parsers;

/// <summary>Разбор чисел, времени и размеров памяти из текста конфигов.</summary>
internal static class ValueParser
{
    private static readonly Regex SizeRegex =
        new(@"^(\d+(?:\.\d+)?)\s*(b|[kmg]b?)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static int? ToInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return (int)Math.Round(d);
        return null;
    }

    public static long? ToLong(string? text) =>
        long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>
    /// «2.0s», «500ms», «2» → миллисекунды.
    /// Число без суффикса считается секундами, если plainIsSeconds = true, иначе миллисекундами.
    /// </summary>
    public static int? ToMilliseconds(string? text, bool plainIsSeconds)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var s = text.Trim().ToLowerInvariant();

        double multiplier;
        if (s.EndsWith("ms")) { multiplier = 1; s = s[..^2]; }
        else if (s.EndsWith('s')) { multiplier = 1000; s = s[..^1]; }
        else multiplier = plainIsSeconds ? 1000 : 1;

        if (!double.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) return null;
        return (int)Math.Round(value * multiplier);
    }

    /// <summary>
    /// «512M», «64K», «1G», «100B», «256MB» → байты.
    /// Число без суффикса умножается на plainUnitBytes (в CATS это мегабайты, в EJUDGE — байты).
    /// </summary>
    public static long? ToBytes(string? text, long plainUnitBytes)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var m = SizeRegex.Match(text.Trim());
        if (!m.Success) return null;

        double value = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        long unit = m.Groups[2].Success && m.Groups[2].Length > 0
            ? char.ToLowerInvariant(m.Groups[2].Value[0]) switch
            {
                'k' => 1024L,
                'm' => 1024L * 1024,
                'g' => 1024L * 1024 * 1024,
                _ => 1L
            }
            : plainUnitBytes;

        return (long)Math.Round(value * unit);
    }
}
