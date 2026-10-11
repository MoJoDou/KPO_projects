using System.Xml.Linq;
using Parser.Abstractions;
using Parser.Models;

namespace Parser.Parsers;

/// <summary>
/// Разбор problem.xml формата PCMS2, вариант Polygon:
/// внутри &lt;judging&gt; лежат &lt;testset&gt; с лимитами и тестами, а проверяющие файлы описаны в &lt;assets&gt;.
/// </summary>
public sealed class Pcms2PolygonParser : IXmlConfigParser
{
    public bool CanParse(XDocument document) =>
        document.Root is { } root
        && root.Name.LocalName == "problem"
        && root.Element("judging") is { } judging
        && judging.Element("script") == null;

    public ProblemInfo Parse(XDocument document)
    {
        var root = document.Root!;
        var info = new ProblemInfo { Id = root.Attr("short-name") };

        info.AddProperty("Вариант problem.xml", "Polygon");
        info.AddProperty("Ревизия", root.Attr("revision"));
        info.AddProperty("Адрес в Polygon", root.Attr("url"));

        ReadTexts(root, info);
        ReadJudging(root.Element("judging")!, info);
        ReadAssets(root, info);

        info.AddProperty("Теги", string.Join(", ", root.Element("tags")?.Elements("tag").Select(t => t.Attr("value")) ?? []));
        return info;
    }

    private static void ReadTexts(XElement root, ProblemInfo info)
    {
        var names = root.Element("names")?.Elements("name").ToList() ?? new List<XElement>();
        info.Title = names.FirstOrDefault()?.Attr("value");
        info.AddProperty("Языки названия", string.Join(", ", names.Select(n => n.Attr("language"))));

        // Условие: предпочитаем исходник TeX, остальные форматы показываем отдельно
        var statements = root.Element("statements")?.Elements("statement").ToList() ?? new List<XElement>();
        var main = statements.FirstOrDefault(s => s.Attr("type") == "application/x-tex") ?? statements.FirstOrDefault();
        info.Statement = main?.Attr("path");
        info.AddProperty("Другие файлы условия",
            string.Join(", ", statements.Where(s => s != main).Select(s => s.Attr("path"))));

        info.AddProperty("Разбор задачи",
            string.Join(", ", root.Element("tutorials")?.Elements("tutorial").Select(t => t.Attr("path")) ?? []));
    }

    private static void ReadJudging(XElement judging, ProblemInfo info)
    {
        // Пустые input-file / output-file означают стандартный ввод/вывод
        info.InputFile = judging.Attr("input-file") ?? "stdin";
        info.OutputFile = judging.Attr("output-file") ?? "stdout";

        var cpu = judging.Attr("cpu-name");
        var speed = judging.Attr("cpu-speed") ?? judging.Attr("cpuspeed");
        info.AddProperty("Процессор для замеров", cpu == null ? null : speed == null ? cpu : $"{cpu}, {speed} МГц");

        var testsets = judging.Elements("testset").ToList();
        bool isFirst = true;

        foreach (var set in testsets)
        {
            var name = set.Attr("name") ?? "tests";
            int? time = set.ChildInt("time-limit");           // миллисекунды
            long? memory = set.ChildLong("memory-limit");     // байты

            if (isFirst)   // общие лимиты берём из первого набора, остальные покажем в свойствах
            {
                info.TimeLimitMs = time;
                info.MemoryLimitBytes = memory;
                isFirst = false;
            }
            if (testsets.Count > 1)
                info.AddProperty($"Набор тестов «{name}»",
                    $"время: {UnitFormat.Milliseconds(time)}, память: {UnitFormat.Bytes(memory)}");

            ReadTestSet(set, name, info);
        }
    }

    private static void ReadTestSet(XElement set, string setName, ProblemInfo info)
    {
        var inputPattern = set.ChildText("input-path-pattern");
        var answerPattern = set.ChildText("answer-path-pattern");
        var groups = new Dictionary<string, TestGroup>();

        int number = 0;
        foreach (var t in set.Element("tests")?.Elements("test") ?? [])
        {
            number++;
            var test = new TestInfo
            {
                Number = number,
                TestSet = setName,
                InputFile = inputPattern == null ? null : NamePattern.Apply(inputPattern, number),
                AnswerFile = answerPattern == null ? null : NamePattern.Apply(answerPattern, number),
                GroupId = t.Attr("group"),
                Points = t.IntAttr("points"),
                IsSample = t.Attr("sample") == "true",
                Note = DescribeOrigin(t)
            };
            info.Tests.Add(test);

            if (test.GroupId == null) continue;
            if (!groups.TryGetValue(test.GroupId, out var group))
            {
                groups[test.GroupId] = group = new TestGroup { Id = test.GroupId, TestSet = setName };
                info.Groups.Add(group);
            }
            group.TestNumbers.Add(number);
        }
    }

    private static string? DescribeOrigin(XElement test) => test.Attr("method") switch
    {
        "manual" => "вручную",
        "generated" => "генератор: " + (test.Attr("cmd") ?? "?"),
        { } other => other,
        null => null
    };

    private static void ReadAssets(XElement root, ProblemInfo info)
    {
        var assets = root.Element("assets");
        if (assets == null) return;

        if (assets.Element("checker") is { } checker)
        {
            var path = checker.Element("source")?.Attr("path");
            var details = string.Join(", ", new[] { checker.Attr("name"), checker.Attr("type") }.Where(s => s != null));
            info.Checker = path == null ? details : $"{path} ({details})";
        }

        info.Interactor = assets.Element("interactor")?.Element("source")?.Attr("path");
        info.Validator = string.Join(", ", assets.Element("validators")?.Elements("validator")
            .Select(v => v.Element("source")?.Attr("path")).Where(p => p != null).Distinct() ?? []);
        if (info.Validator.Length == 0) info.Validator = null;

        foreach (var s in assets.Element("solutions")?.Elements("solution") ?? [])
        {
            var path = s.Element("source")?.Attr("path");
            info.Solutions.Add(new SolutionInfo
            {
                Name = path == null ? "?" : Path.GetFileNameWithoutExtension(path),
                Path = path,
                Tag = s.Attr("tag")
            });
        }

        // Генераторы и прочие утилиты из <files>/<executables>
        info.AddProperty("Исходники утилит (генератор и др.)", string.Join(", ",
            root.Element("files")?.Element("executables")?.Elements("executable")
                .Select(e => e.Element("source")?.Attr("path")).Where(p => p != null) ?? []));
    }
}
