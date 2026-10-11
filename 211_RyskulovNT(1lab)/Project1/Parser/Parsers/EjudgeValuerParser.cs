using System.Text.RegularExpressions;
using Parser.Abstractions;
using Parser.Infrastructure;

namespace Parser.Parsers;

/// <summary>
/// Разбор valuer.cfg по грамматике из описания формата:
///   global { опция; ... }   group ID { tests 1-5; score 10; requires 0, 1; ... }
/// Файл свободного формата, комментарии начинаются с «#».
/// </summary>
public sealed class EjudgeValuerParser : ITextConfigParser<ValuerConfig>
{
    private static readonly Regex Comment = new(@"#[^\r\n]*", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"[{};,]|[^\s{};,]+", RegexOptions.Compiled);

    private sealed record Statement(string Name, List<string> Args);

    public ValuerConfig Parse(string text)
    {
        var tokens = Token.Matches(Comment.Replace(text, "")).Select(m => m.Value).ToList();
        var config = new ValuerConfig();
        int pos = 0;

        while (pos < tokens.Count)
        {
            string word = tokens[pos++];

            if (word == "global")
            {
                foreach (var st in ReadBlock(tokens, ref pos))
                    config.GlobalOptions.Add(st.Args.Count == 0 ? st.Name : $"{st.Name} {string.Join(" ", st.Args)}");
            }
            else if (word == "group")
            {
                if (pos >= tokens.Count) throw new PackageException("valuer.cfg: после 'group' не указан номер группы.");
                var group = new ValuerGroup { Id = tokens[pos++] };
                foreach (var st in ReadBlock(tokens, ref pos))
                    ApplyOption(group, st);
                config.Groups.Add(group);
            }
            else
            {
                throw new PackageException($"valuer.cfg: неожиданное слово '{word}' (ожидалось 'global' или 'group').");
            }
        }
        return config;
    }

    /// <summary>Читает «{ оператор; оператор; ... }». Оператор — имя и аргументы до «;».</summary>
    private static List<Statement> ReadBlock(List<string> tokens, ref int pos)
    {
        if (pos >= tokens.Count || tokens[pos] != "{")
            throw new PackageException("valuer.cfg: ожидалась открывающая '{'.");
        pos++;

        var statements = new List<Statement>();
        while (true)
        {
            if (pos >= tokens.Count) throw new PackageException("valuer.cfg: не хватает закрывающей '}'.");
            if (tokens[pos] == "}") { pos++; return statements; }
            if (tokens[pos] == ";") { pos++; continue; }

            string name = tokens[pos++];
            var args = new List<string>();
            while (pos < tokens.Count && tokens[pos] != ";" && tokens[pos] != "}")
                args.Add(tokens[pos++]);
            if (pos < tokens.Count && tokens[pos] == ";") pos++;

            statements.Add(new Statement(name, args));
        }
    }

    private static void ApplyOption(ValuerGroup group, Statement st)
    {
        switch (st.Name)
        {
            case "tests":      // «1-5», допускаются пробелы вокруг «-»
                group.Tests.AddRange(RangeSpec.Parse(string.Concat(st.Args)));
                break;
            case "score":
                group.Score = ValueParser.ToInt(st.Args.FirstOrDefault());
                break;
            case "test_score":
                group.TestScore = ValueParser.ToInt(st.Args.FirstOrDefault());
                break;
            case "requires":
                group.Requires.AddRange(st.Args.Where(a => a != ","));
                break;
            default:
                group.OtherOptions.Add(st.Args.Count == 0 ? st.Name : $"{st.Name} {string.Join(" ", st.Args)}");
                break;
        }
    }
}
