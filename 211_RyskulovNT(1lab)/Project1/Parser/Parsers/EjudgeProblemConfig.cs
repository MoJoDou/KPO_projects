namespace Parser.Parsers;

/// <summary>Набор настроек из problem.cfg: «ключ = значение».</summary>
public sealed class EjudgeProblemConfig
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, string value) => _values[key] = value;

    public string? Get(string key) => _values.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

    /// <summary>Логическая настройка: 1 / true / yes.</summary>
    public bool GetFlag(string key) =>
        Get(key) is { } v && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase));
}
