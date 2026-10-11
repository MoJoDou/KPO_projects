using System.Xml;
using System.Xml.Linq;
using Parser.Abstractions;
using Parser.Infrastructure;
using Parser.Models;

namespace Parser.Handlers;

/// <summary>
/// Общая часть обработчиков XML-форматов (шаблонный метод):
/// найти в пакете файлы описания → загрузить XML → выбрать подходящий парсер → дополнить результат.
/// Конкретный формат задаёт только то, какие файлы считать описанием задачи.
/// </summary>
public abstract class XmlFormatHandler : IFormatHandler
{
    private readonly IReadOnlyList<IXmlConfigParser> _parsers;

    protected XmlFormatHandler(string formatName, IEnumerable<IXmlConfigParser> parsers)
    {
        FormatName = formatName;
        _parsers = parsers.ToList();
    }

    public string FormatName { get; }

    /// <summary>Может ли этот файл быть описанием задачи.</summary>
    protected abstract bool IsConfigFile(string path);

    /// <summary>Какие из найденных кандидатов брать. По умолчанию — все.</summary>
    protected virtual IEnumerable<string> SelectConfigFiles(IReadOnlyList<string> candidates) => candidates;

    public IReadOnlyList<ProblemInfo> Handle(IPackage package)
    {
        var problems = new List<ProblemInfo>();

        foreach (var path in SelectConfigFiles(package.Files.Where(IsConfigFile).ToList()))
        {
            var document = LoadXml(package, path);

            var parser = _parsers.FirstOrDefault(p => p.CanParse(document));
            if (parser == null) continue;   // XML другого формата — пропускаем

            var problem = parser.Parse(document);
            problem.Format = FormatName;
            problem.ConfigFile = path;
            problem.Location = PackagePath.DirectoryOf(path);
            problems.Add(problem);
        }

        if (problems.Count == 0)
            throw new PackageException($"В пакете '{package.Name}' не найдено описание задачи в формате {FormatName}.");

        return problems;
    }

    private static XDocument LoadXml(IPackage package, string path)
    {
        try
        {
            using var stream = package.OpenRead(path);
            return XDocument.Load(stream);   // кодировку берёт из заголовка <?xml ... encoding="..."?>
        }
        catch (XmlException ex)
        {
            throw new PackageException($"Файл '{path}' содержит некорректный XML: {ex.Message}", ex);
        }
    }
}
