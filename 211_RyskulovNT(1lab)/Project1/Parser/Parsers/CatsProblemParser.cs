using System.Text.RegularExpressions;
using System.Xml.Linq;
using Parser.Abstractions;
using Parser.Infrastructure;
using Parser.Models;

namespace Parser.Parsers;

/// <summary>Разбор XML-файла задачи формата CATS (корневой тег &lt;CATS&gt;).</summary>
public sealed class CatsProblemParser : IXmlConfigParser
{
    private const long Megabyte = 1024 * 1024;   // в CATS память без суффикса — мегабайты

    public bool CanParse(XDocument document) => document.Root?.Name.LocalName == "CATS";

    public ProblemInfo Parse(XDocument document)
    {
        var root = document.Root!;
        var problem = root.Element("Problem")
            ?? throw new PackageException("CATS: в файле нет обязательного тега <Problem>.");

        var info = new ProblemInfo
        {
            Title = problem.Attr("title"),
            Author = problem.Attr("author"),
            InputFile = problem.Attr("inputFile"),
            OutputFile = problem.Attr("outputFile"),
            TimeLimitMs = ValueParser.ToMilliseconds(problem.Attr("tlimit"), plainIsSeconds: true),
            MemoryLimitBytes = ValueParser.ToBytes(problem.Attr("mlimit"), Megabyte),
            DeclaredMaxScore = problem.IntAttr("maxPoints")
        };

        ReadGeneral(root, problem, info);
        ReadParts(problem, info);
        ReadTests(problem, info);
        return info;
    }

    // ---- общие данные ----

    private static void ReadGeneral(XElement root, XElement problem, ProblemInfo info)
    {
        info.AddProperty("Версия формата CATS", root.Attr("version"));
        info.AddProperty("Языки условия", problem.Attr("lang"));
        info.AddProperty("Сложность (оценка автора)", problem.Attr("difficulty"));

        var writeLimit = ValueParser.ToBytes(problem.Attr("wlimit"), Megabyte);
        if (writeLimit != null) info.AddProperty("Лимит записи", UnitFormat.Bytes(writeLimit));

        info.AddProperty("Ключевые слова", string.Join(", ", problem.Elements("Keyword").Select(k => k.Attr("code"))));
        info.AddProperty("Способ запуска", problem.Element("Run")?.Attr("method"));

        // Условие: либо текст в XML, либо ссылка/вложение
        var statement = problem.Element("ProblemStatement");
        if (statement != null)
        {
            info.Statement = statement.Attr("url") is { } url ? $"ссылка: {url}"
                : statement.Attr("attachment") is { } att ? $"вложение: {att}"
                : "встроено в XML (формат STML)";
        }

        var constraints = problem.Element("ProblemConstraints")?.Value;
        if (constraints != null)
            info.AddProperty("Ограничения на входные данные", Regex.Replace(constraints, @"\s+", " "));

        if (problem.Element("Explanation") != null) info.AddProperty("Разбор задачи", "есть");

        int samples = problem.Elements("Sample").Sum(s => RangeSpec.Parse(s.Attr("rank")).Count);
        if (samples > 0) info.AddProperty("Примеров в условии", samples.ToString());

        info.AddProperty("Картинки", string.Join(", ", problem.Elements("Picture").Select(p => $"{p.Attr("name")} ({p.Attr("src")})")));
        info.AddProperty("Вложения", string.Join(", ", problem.Elements("Attachment").Select(a => $"{a.Attr("name")} ({a.Attr("src")})")));
    }

    // ---- проверяющая программа, решения, генераторы ----

    private static void ReadParts(XElement problem, ProblemInfo info)
    {
        // Стандартная программа проверки (атрибут stdChecker) важнее тега <Checker>
        var stdChecker = problem.Attr("stdChecker");
        var checker = problem.Element("Checker");
        if (stdChecker != null)
            info.Checker = $"стандартная: {stdChecker}";
        else if (checker != null)
            info.Checker = $"{checker.Attr("src")} (style: {checker.Attr("style") ?? "legacy"})";

        var imports = problem.Elements("Import").ToList();
        info.AddProperty("Импорт из других задач",
            string.Join(", ", imports.Select(i => i.Attr("type") is { } t ? $"{i.Attr("guid")} ({t})" : i.Attr("guid"))));
        if (info.Checker == null)
            info.Checker = imports.FirstOrDefault(i => i.Attr("type") == "checker")?.Attr("guid");

        info.Interactor = problem.Element("Interactor")?.Attr("src");
        info.Validator = problem.Element("Validator")?.Attr("src");

        foreach (var s in problem.Elements("Solution"))
            info.Solutions.Add(new SolutionInfo { Name = s.Attr("name") ?? "", Path = s.Attr("src") });

        var generators = problem.Elements("Generator").Select(g => $"{g.Attr("name")} ({g.Attr("src")})")
            .Concat(problem.Elements("GeneratorRange")
                .Select(g => $"{g.Attr("name")} [{g.Attr("from")}-{g.Attr("to")}] ({g.Attr("src")})"));
        info.AddProperty("Генераторы тестов", string.Join(", ", generators));
    }

    // ---- тесты ----

    private static void ReadTests(XElement problem, ProblemInfo info)
    {
        var tests = new SortedDictionary<int, TestInfo>();
        int? defaultPoints = null;                // параметры из <Test rank="*"> — для всех тестов
        XElement? defaultIn = null, defaultOut = null;

        foreach (var el in problem.Elements().Where(e => e.Name.LocalName is "Test" or "TestRange"))
        {
            var inEl = el.Element("In");
            var outEl = el.Element("Out");
            int? points = el.IntAttr("points");

            IEnumerable<int> numbers;
            if (el.Name.LocalName == "TestRange")                     // устаревший тег
                numbers = RangeSpec.Between(el.IntAttr("from"), el.IntAttr("to"));
            else if (el.Attr("rank") == "*")
            {
                defaultPoints = points; defaultIn = inEl; defaultOut = outEl;
                continue;
            }
            else
                numbers = RangeSpec.Parse(el.Attr("rank"));

            foreach (int n in numbers)
            {
                if (!tests.TryGetValue(n, out var test))
                    tests[n] = test = new TestInfo { Number = n };
                FillTest(test, points, inEl, outEl);          // данные одного теста могут быть в разных тегах
            }
        }

        foreach (var test in tests.Values)
            FillTest(test, defaultPoints, defaultIn, defaultOut);   // что не задано — берём из rank="*"

        // Наборы тестов <Testset> → группы
        foreach (var set in problem.Elements("Testset"))
        {
            var numbers = RangeSpec.Parse(set.Attr("tests"), out var includedSets);
            var group = new TestGroup
            {
                Id = set.Attr("name") ?? "?",
                Name = set.Attr("comment"),
                Points = set.IntAttr("points")
            };
            group.TestNumbers.AddRange(numbers);
            group.Prerequisites.AddRange((set.Attr("depends_on") ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

            var notes = new List<string>();
            if (includedSets.Count > 0) notes.Add("включает наборы: " + string.Join(", ", includedSets));
            if (set.Attr("hideDetails") is { } hide) notes.Add("hideDetails=" + hide);
            group.Note = notes.Count > 0 ? string.Join("; ", notes) : null;

            info.Groups.Add(group);
            foreach (int n in numbers)
                if (tests.TryGetValue(n, out var t)) t.GroupId ??= group.Id;
        }

        info.Tests.AddRange(tests.Values);
    }

    /// <summary>Заполняет только те поля теста, которые ещё пусты (данные не перекрываются).</summary>
    private static void FillTest(TestInfo test, int? points, XElement? inEl, XElement? outEl)
    {
        test.Points ??= points;
        test.InputFile ??= DescribeInput(inEl, test.Number);
        test.AnswerFile ??= DescribeAnswer(outEl, test.Number);
    }

    private static string? DescribeInput(XElement? el, int number)
    {
        if (el == null) return null;
        if (el.Attr("src") is { } src) return NamePattern.Apply(src, number);
        if (el.Attr("use") is { } generator)
        {
            var param = el.Attr("param");
            return param == null ? $"генератор {generator}" : $"генератор {generator} {NamePattern.Apply(param, number)}";
        }
        return string.IsNullOrWhiteSpace(el.Value) ? null : "текст в XML";
    }

    private static string? DescribeAnswer(XElement? el, int number)
    {
        if (el == null) return null;
        if (el.Attr("src") is { } src) return NamePattern.Apply(src, number);
        if (el.Attr("use") is { } solution) return $"решение {solution}";
        return string.IsNullOrWhiteSpace(el.Value) ? null : "текст в XML";
    }
}
