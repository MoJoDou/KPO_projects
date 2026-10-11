using System.Text.RegularExpressions;

namespace Parser.Parsers;

/// <summary>
/// Подстановка номера теста в шаблон имени файла. Поддерживаются шаблоны всех форматов:
/// «%02d» и «%d» (PCMS2/EJUDGE), «%n» и «%0n» (CATS), «##» (PCMS2, олимпиадный вариант).
/// </summary>
internal static class NamePattern
{
    private static readonly Regex Token = new(@"%(0?)(\d*)([dn])|#+", RegexOptions.Compiled);

    public static string Apply(string pattern, int number) =>
        Token.Replace(pattern, m =>
        {
            if (m.Value[0] == '#')                       // «##» → ширина равна числу решёток
                return number.ToString().PadLeft(m.Length, '0');

            bool zeroFlag = m.Groups[1].Length > 0;
            int width = m.Groups[2].Length > 0
                ? int.Parse(m.Groups[2].Value)
                : (zeroFlag && m.Groups[3].Value == "n" ? 2 : 0);   // «%0n» = два знака с нулём

            return number.ToString().PadLeft(width, '0');
        });
}
