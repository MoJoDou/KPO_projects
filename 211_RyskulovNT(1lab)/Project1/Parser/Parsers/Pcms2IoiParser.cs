using System.Xml.Linq;
using Parser.Abstractions;
using Parser.Models;

namespace Parser.Parsers;

/// <summary>
/// Разбор problem.xml формата PCMS2, олимпиадный вариант:
/// &lt;judging&gt;&lt;script&gt;&lt;testset&gt;&lt;test-group&gt;..., а программа проверки задаётся в &lt;verifier&gt;.
/// </summary>
public sealed class Pcms2IoiParser : IXmlConfigParser
{
    public bool CanParse(XDocument document) =>
        document.Root is { } root
        && root.Name.LocalName == "problem"
        && root.Element("judging")?.Element("script") != null;

    public ProblemInfo Parse(XDocument document)
    {
        var root = document.Root!;
        var script = root.Element("judging")!.Element("script")!;

        var info = new ProblemInfo { Id = root.Attr("id") };
        info.AddProperty("Вариант problem.xml", "олимпиадный (judging/script)");
        info.AddProperty("Версия формата", root.Attr("version"));
        info.AddProperty("Тип оценивания (script)", script.Attr("type"));

        info.Checker = Describe(script.Element("verifier"));
        info.Interactor = Describe(script.Element("interactor"));

        var testsets = script.Elements("testset").ToList();
        bool isFirst = true;

        foreach (var set in testsets)
        {
            var name = set.Attr("name") ?? "tests";
            int? time = ValueParser.ToMilliseconds(set.Attr("time-limit"), plainIsSeconds: true);
            long? memory = ValueParser.ToBytes(set.Attr("memory-limit"), plainUnitBytes: 1);

            if (isFirst)
            {
                info.InputFile = set.Attr("input-name");
                info.OutputFile = set.Attr("output-name");
                info.TimeLimitMs = time;
                info.MemoryLimitBytes = memory;
                isFirst = false;
            }

            int before = info.Tests.Count;
            ReadTestSet(set, name, info);

            if (testsets.Count > 1)
                info.AddProperty($"Набор тестов «{name}»",
                    $"тестов: {info.Tests.Count - before}, время: {UnitFormat.Milliseconds(time)}, память: {UnitFormat.Bytes(memory)}");
        }

        return info;
    }

    private static void ReadTestSet(XElement set, string setName, ProblemInfo info)
    {
        var inputPattern = set.Attr("input-href");
        var answerPattern = set.Attr("answer-href");

        int number = 0;      // нумерация тестов идёт заново в каждом наборе
        int groupIndex = 0;  // группы нумеруются по порядку, с единицы (на них ссылается require-groups)

        // Внутри набора тесты лежат либо сами по себе, либо внутри <test-group>
        foreach (var el in set.Elements())
        {
            if (el.Name.LocalName == "test")
            {
                AddTest(info, el, ++number, setName, inputPattern, answerPattern, groupId: null);
            }
            else if (el.Name.LocalName == "test-group")
            {
                groupIndex++;
                var group = CreateGroup(el, groupIndex, setName);
                info.Groups.Add(group);

                foreach (var t in el.Elements("test"))
                {
                    number++;
                    AddTest(info, t, number, setName, inputPattern, answerPattern, group.Id);
                    group.TestNumbers.Add(number);
                }
            }
        }
    }

    private static TestGroup CreateGroup(XElement el, int index, string setName)
    {
        var group = new TestGroup
        {
            Id = index.ToString(),
            TestSet = setName,
            Name = el.Attr("comment"),
            Points = el.IntAttr("group-bonus")     // баллы за полное прохождение группы
        };

        group.Prerequisites.AddRange((el.Attr("require-groups") ?? "")
            .Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));
        if (el.Attr("require-previous") == "true" && index > 1)
            group.Prerequisites.Add((index - 1).ToString());

        var notes = new List<string>();
        if (el.Attr("score") is { } score) notes.Add("score=" + score);
        if (el.Attr("feedback") is { } feedback) notes.Add("feedback=" + feedback);
        group.Note = notes.Count > 0 ? string.Join(", ", notes) : null;
        return group;
    }

    private static void AddTest(ProblemInfo info, XElement el, int number, string setName,
        string? inputPattern, string? answerPattern, string? groupId)
    {
        info.Tests.Add(new TestInfo
        {
            Number = number,
            TestSet = setName,
            GroupId = groupId,
            Points = el.IntAttr("points"),
            InputFile = inputPattern == null ? null : NamePattern.Apply(inputPattern, number),
            AnswerFile = answerPattern == null ? null : NamePattern.Apply(answerPattern, number),
            Note = el.Attr("comment")
        });
    }

    /// <summary>«check.exe (тип %alt)» из тега &lt;verifier&gt; / &lt;interactor&gt;.</summary>
    private static string? Describe(XElement? el)
    {
        if (el == null) return null;
        var file = el.Element("binary")?.Attr("file");
        var type = el.Attr("type");
        return type == null ? file : $"{file} (тип {type})";
    }
}
