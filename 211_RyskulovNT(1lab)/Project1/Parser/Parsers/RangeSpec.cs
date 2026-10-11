using Parser.Infrastructure;

namespace Parser.Parsers;

/// <summary>
/// Спецификация номеров тестов: «1,3,5-8,10-20-2».
/// Элемент — номер, диапазон «от-до» или диапазон с шагом «от-до-шаг».
/// Всё, что не число (например, имя набора тестов), возвращается отдельно в names.
/// </summary>
internal static class RangeSpec
{
    private const int MaxRange = 100_000; // защита от «1-2000000000» в испорченном файле

    public static List<int> Parse(string? spec) => Parse(spec, out _);

    public static List<int> Parse(string? spec, out List<string> names)
    {
        var numbers = new List<int>();
        names = new List<string>();
        if (string.IsNullOrWhiteSpace(spec)) return numbers;

        foreach (var item in spec.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = item.Split('-');
            var values = new int[parts.Length];
            bool allNumbers = parts.Length <= 3 && parts.Select((p, i) => int.TryParse(p, out values[i])).All(ok => ok);

            if (!allNumbers) { names.Add(item); continue; }

            switch (parts.Length)
            {
                case 1: numbers.Add(values[0]); break;
                case 2: numbers.AddRange(Between(values[0], values[1])); break;
                default: numbers.AddRange(Between(values[0], values[1], values[2])); break;
            }
        }
        return numbers;
    }

    public static IEnumerable<int> Between(int? from, int? to, int step = 1)
    {
        if (from is null || to is null || from > to) yield break;
        if (step < 1) step = 1;
        if ((long)to.Value - from.Value > MaxRange)
            throw new PackageException($"Слишком большой диапазон тестов: {from}-{to}.");

        for (int n = from.Value; n <= to.Value; n += step)
            yield return n;
    }
}
