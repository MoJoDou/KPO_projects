namespace Parser.Infrastructure;

/// <summary>Работа с путями внутри пакета (всегда с разделителем «/»).</summary>
public static class PackagePath
{
    public static string DirectoryOf(string path)
    {
        int i = path.LastIndexOf('/');
        return i < 0 ? "" : path[..i];
    }

    public static string FileNameOf(string path)
    {
        int i = path.LastIndexOf('/');
        return i < 0 ? path : path[(i + 1)..];
    }

    /// <summary>Склеивает папку и относительный путь; пустая папка означает корень пакета.</summary>
    public static string Combine(string directory, string relative) =>
        directory.Length == 0 ? relative : directory + "/" + relative;

    /// <summary>Глубина вложенности: «a.txt» = 0, «dir/a.txt» = 1.</summary>
    public static int Depth(string path) => path.Count(c => c == '/');
}
