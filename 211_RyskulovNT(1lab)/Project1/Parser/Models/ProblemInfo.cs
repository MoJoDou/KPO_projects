namespace Parser.Models;

/// <summary>
/// Единая модель задачи. Каждый формат (KRSU, CATS, PCMS2, EJUDGE) приводится к ней,
/// поэтому вывод (принтер) ничего не знает о форматах.
/// </summary>
public sealed class ProblemInfo
{
    // Откуда взята задача (заполняет обработчик формата)
    public string Format { get; set; } = "";
    public string ConfigFile { get; set; } = "";
    public string Location { get; set; } = "";

    // Общие данные
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Statement { get; set; }
    public string? InputFile { get; set; }
    public string? OutputFile { get; set; }
    public string? Checker { get; set; }
    public string? Interactor { get; set; }
    public string? Validator { get; set; }

    // Ограничения (единые единицы: миллисекунды и байты)
    public int? TimeLimitMs { get; set; }
    public long? MemoryLimitBytes { get; set; }

    // Баллы
    public int? DeclaredMaxScore { get; set; }

    public List<TestInfo> Tests { get; } = new();
    public List<TestGroup> Groups { get; } = new();
    public List<SolutionInfo> Solutions { get; } = new();
    public List<ProblemProperty> Properties { get; } = new();

    public int SampleCount => Tests.Count(t => t.IsSample);

    /// <summary>
    /// Максимальный балл: заявленный в файле, иначе сумма баллов групп, иначе сумма баллов тестов.
    /// </summary>
    public int? MaxScore
    {
        get
        {
            if (DeclaredMaxScore.HasValue) return DeclaredMaxScore;
            if (Groups.Any(g => g.Points.HasValue)) return Groups.Sum(g => g.Points ?? 0);
            if (Tests.Any(t => t.Points.HasValue)) return Tests.Sum(t => t.Points ?? 0);
            return null;
        }
    }

    /// <summary>Добавляет свойство, если у него есть значение.</summary>
    public void AddProperty(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            Properties.Add(new ProblemProperty(name, value.Trim()));
    }
}
