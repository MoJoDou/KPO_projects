namespace Parser.Abstractions;

/// <summary>
/// Пакет задачи (архив). Обработчики форматов работают только с этим интерфейсом,
/// поэтому им всё равно, ZIP это, папка на диске или заглушка в тестах.
/// </summary>
public interface IPackage : IDisposable
{
    string Name { get; }

    /// <summary>Пути всех файлов внутри пакета (разделитель «/», папки не включены).</summary>
    IReadOnlyList<string> Files { get; }

    bool Exists(string path);

    Stream OpenRead(string path);
}
