using System.Text;
using Parser.Abstractions;

namespace Parser.Infrastructure;

public static class PackageExtensions
{
    /// <summary>Читает текстовый файл пакета (UTF-8, BOM распознаётся автоматически).</summary>
    public static string ReadAllText(this IPackage package, string path)
    {
        using var stream = package.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }
}
