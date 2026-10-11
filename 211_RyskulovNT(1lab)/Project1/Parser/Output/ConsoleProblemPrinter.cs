using Parser.Abstractions;
using Parser.Models;

namespace Parser.Output;

/// <summary>Печатает информацию о задачах в текстовом виде (в консоль или в любой TextWriter).</summary>
public sealed class ConsoleProblemPrinter : IProblemPrinter
{
    private const int LabelWidth = 32;
    private const int MaxCellWidth = 40;

    private readonly TextWriter _out;
    private readonly int _maxTestRows;

    public ConsoleProblemPrinter(TextWriter output, int maxTestRows = 10)
    {
        _out = output;
        _maxTestRows = maxTestRows;
    }

    public void Print(string packageName, IReadOnlyList<ProblemInfo> problems)
    {
        _out.WriteLine($"Пакет: {packageName}");
        _out.WriteLine($"Найдено задач: {problems.Count}");

        for (int i = 0; i < problems.Count; i++)
        {
            _out.WriteLine();
            _out.WriteLine(new string('=', 78));
            _out.WriteLine($"Задача {i + 1} из {problems.Count}");
            _out.WriteLine(new string('=', 78));
            PrintProblem(problems[i]);
        }
    }

    private void PrintProblem(ProblemInfo p)
    {
        Line("Формат", p.Format);
        Line("Файл описания", p.ConfigFile);
        Line("Папка задачи", p.Location.Length == 0 ? "(корень пакета)" : p.Location);
        Line("Идентификатор", p.Id);
        Line("Название", p.Title ?? "не указано");
        Line("Автор", p.Author);
        Line("Файл условия", p.Statement);
        Line("Входной файл", p.InputFile);
        Line("Выходной файл", p.OutputFile);
        Line("Лимит времени", UnitFormat.Milliseconds(p.TimeLimitMs));
        Line("Лимит памяти", UnitFormat.Bytes(p.MemoryLimitBytes));
        Line("Проверяющая программа", p.Checker ?? "не указана");
        Line("Интерактор", p.Interactor);
        Line("Валидатор", p.Validator);
        Line("Максимальный балл", p.MaxScore?.ToString() ?? "не задан");
        Line("Количество тестов", p.Tests.Count + (p.SampleCount > 0 ? $" (из них примеров: {p.SampleCount})" : ""));

        foreach (var property in p.Properties)
            Line(property.Name, property.Value);

        PrintSolutions(p);
        PrintGroups(p);
        PrintTests(p);
    }

    private void Line(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        _out.WriteLine($"{label + ":",-LabelWidth} {value}");
    }

    private void PrintSolutions(ProblemInfo p)
    {
        if (p.Solutions.Count == 0) return;
        _out.WriteLine();
        _out.WriteLine("Решения:");
        foreach (var s in p.Solutions)
        {
            var path = s.Path == null || s.Path == s.Name ? "" : $" -> {s.Path}";
            var tag = s.Tag == null ? "" : $" [{s.Tag}]";
            _out.WriteLine($"  {s.Name}{path}{tag}");
        }
    }

    private void PrintGroups(ProblemInfo p)
    {
        if (p.Groups.Count == 0) return;
        _out.WriteLine();
        _out.WriteLine("Группы тестов:");

        var rows = p.Groups.Select(g => new[]
        {
            g.TestSet ?? "", g.Id, g.Name ?? "", g.Points?.ToString() ?? "",
            string.Join(", ", g.Prerequisites), FormatRanges(g.TestNumbers), g.Note ?? ""
        }).ToList();

        PrintTable(new[] { "Набор", "Группа", "Название", "Баллы", "Требует", "Тесты", "Примечание" }, rows);
    }

    private void PrintTests(ProblemInfo p)
    {
        if (p.Tests.Count == 0) return;
        _out.WriteLine();
        _out.WriteLine("Тесты:");

        var rows = p.Tests.Take(_maxTestRows).Select(t => new[]
        {
            t.Number.ToString(), t.TestSet ?? "", t.GroupId ?? "", t.Points?.ToString() ?? "",
            t.InputFile ?? "", t.AnswerFile ?? "",
            string.Join("; ", new[] { t.IsSample ? "пример" : null, t.Note }.Where(s => !string.IsNullOrEmpty(s)))
        }).ToList();

        PrintTable(new[] { "№", "Набор", "Группа", "Баллы", "Вход", "Ответ", "Примечание" }, rows);

        int hidden = p.Tests.Count - rows.Count;
        if (hidden > 0)
            _out.WriteLine($"  ... и ещё {hidden} (чтобы показать все тесты, добавьте параметр --all)");
    }

    /// <summary>Таблица с выравниванием; колонки, в которых нигде нет данных, не выводятся.</summary>
    private void PrintTable(string[] headers, List<string[]> rows)
    {
        var columns = Enumerable.Range(0, headers.Length)
            .Where(c => rows.Any(r => r[c].Length > 0))
            .ToList();
        if (columns.Count == 0) return;

        string Cut(string s) => s.Length <= MaxCellWidth ? s : s[..(MaxCellWidth - 1)] + "…";
        var widths = columns.ToDictionary(c => c, c => Math.Max(headers[c].Length, rows.Max(r => Cut(r[c]).Length)));

        string Format(Func<int, string> cell) =>
            "  " + string.Join("  ", columns.Select(c => cell(c).PadRight(widths[c]))).TrimEnd();

        _out.WriteLine(Format(c => headers[c]));
        _out.WriteLine("  " + string.Join("  ", columns.Select(c => new string('-', widths[c]))));
        foreach (var row in rows)
            _out.WriteLine(Format(c => Cut(row[c])));
    }

    /// <summary>[1,2,3,5,7,8] → «1-3, 5, 7-8».</summary>
    private static string FormatRanges(IEnumerable<int> numbers)
    {
        var sorted = numbers.Distinct().OrderBy(n => n).ToList();
        var parts = new List<string>();

        for (int i = 0; i < sorted.Count;)
        {
            int j = i;
            while (j + 1 < sorted.Count && sorted[j + 1] == sorted[j] + 1) j++;
            parts.Add(j == i ? sorted[i].ToString() : $"{sorted[i]}-{sorted[j]}");
            i = j + 1;
        }
        return string.Join(", ", parts);
    }
}
