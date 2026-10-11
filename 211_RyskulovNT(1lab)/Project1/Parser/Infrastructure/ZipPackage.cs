using System.IO.Compression;
using Parser.Abstractions;

namespace Parser.Infrastructure;

/// <summary>Пакет, хранящийся в ZIP-файле. Архив не распаковывается на диск — файлы читаются из памяти.</summary>
public sealed class ZipPackage : IPackage
{
    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public string Name { get; }

    public IReadOnlyList<string> Files { get; }

    public ZipPackage(string zipPath)
    {
        if (!File.Exists(zipPath))
            throw new PackageException($"Файл пакета не найден: {zipPath}");

        Name = Path.GetFileName(zipPath);

        try
        {
            _archive = ZipFile.OpenRead(zipPath);
        }
        catch (InvalidDataException ex)
        {
            throw new PackageException($"Файл '{zipPath}' не является корректным ZIP-архивом.", ex);
        }

        foreach (var entry in _archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;          // это папка
            _entries[entry.FullName.Replace('\\', '/')] = entry;      // некоторые архиваторы пишут «\»
        }

        Files = _entries.Keys.OrderBy(p => p, StringComparer.Ordinal).ToList();
    }

    public bool Exists(string path) => _entries.ContainsKey(path);

    public Stream OpenRead(string path)
    {
        if (!_entries.TryGetValue(path, out var entry))
            throw new PackageException($"В пакете нет файла '{path}'.");

        // Поток внутри архива нельзя перематывать, поэтому отдаём копию в памяти.
        var copy = new MemoryStream();
        using (var source = entry.Open())
            source.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }

    public void Dispose() => _archive.Dispose();
}
