using System.Xml.Linq;
using Parser.Abstractions;
using Parser.Models;

namespace Parser.Parsers;

/// <summary>Разбор файла testinfo.xml формата KRSU.</summary>
public sealed class KrsuTestInfoParser : IXmlConfigParser
{
    public bool CanParse(XDocument document) => document.Root?.Name.LocalName == "testinfo";

    public ProblemInfo Parse(XDocument document)
    {
        var root = document.Root!;

        var info = new ProblemInfo
        {
            Checker = root.ChildText("checker"),
            Interactor = root.ChildText("interactor"),
            Statement = root.ChildText("problem"),
            TimeLimitMs = root.ChildInt("timelimit"),          // в файле миллисекунды
            MemoryLimitBytes = root.ChildLong("memorylimit")   // в файле байты
        };
        info.AddProperty("Версия пакета тестов", root.ChildText("testversion"));
        info.AddProperty("Режим тестирования", DescribeRunType(root.ChildText("runtype")));

        foreach (var g in root.Elements("group"))
        {
            var group = new TestGroup { Id = g.Attr("id") ?? "?", Points = g.IntAttr("points") };
            group.Prerequisites.AddRange(SplitList(g.Attr("prereq")));
            info.Groups.Add(group);
        }

        int number = 0;   // номер теста = его порядковый номер в файле
        foreach (var t in root.Elements("test"))
        {
            number++;
            var test = new TestInfo
            {
                Number = number,
                InputFile = t.Attr("input"),
                AnswerFile = t.Attr("output"),
                GroupId = t.Attr("groupid"),
                Points = t.IntAttr("points")
            };
            info.Tests.Add(test);

            if (test.GroupId != null)
                GetOrAddGroup(info, test.GroupId).TestNumbers.Add(number);
        }

        return info;
    }

    private static TestGroup GetOrAddGroup(ProblemInfo info, string id)
    {
        var group = info.Groups.FirstOrDefault(g => g.Id == id);
        if (group == null)
        {
            group = new TestGroup { Id = id };   // тест ссылается на группу, которую не описали
            info.Groups.Add(group);
        }
        return group;
    }

    private static IEnumerable<string> SplitList(string? text) =>
        (text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string? DescribeRunType(string? value) => value switch
    {
        null => null,
        "0" => "0 — до первого неверного теста",
        "1" => "1 — все тесты",
        "2" => "2 — по группам тестов",
        _ => value
    };
}
