using Parser.Abstractions;

namespace Parser.Parsers;

/// <summary>
/// Разбор problem.cfg (INI-подобный файл): секция [problem], строки «ключ = значение»,
/// значения могут быть в кавычках, строки с # — комментарии.
/// </summary>
public sealed class EjudgeProblemCfgParser : ITextConfigParser<EjudgeProblemConfig>
{
    public EjudgeProblemConfig Parse(string text)
    {
        var config = new EjudgeProblemConfig();
        string section = "";

        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

            if (line[0] == '[' && line[^1] == ']')
            {
                section = line[1..^1].Trim();
                continue;
            }
            if (section.Length > 0 && !section.Equals("problem", StringComparison.OrdinalIgnoreCase)) continue;

            int eq = line.IndexOf('=');
            string key = (eq < 0 ? line : line[..eq]).Trim();
            string value = eq < 0 ? "1" : line[(eq + 1)..].Trim();   // ключ без «=» — это включённый флаг

            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];

            config.Set(key, value);
        }
        return config;
    }
}
