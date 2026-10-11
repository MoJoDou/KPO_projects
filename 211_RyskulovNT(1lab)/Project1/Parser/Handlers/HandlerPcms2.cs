using Parser.Abstractions;
using Parser.Infrastructure;

namespace Parser.Handlers;

/// <summary>
/// Формат PCMS2: описание задачи — файл problem.xml. У него два варианта разметки (Polygon и олимпиадный),
/// поэтому обработчик получает несколько парсеров и сам выбирает подходящий.
/// </summary>
public sealed class HandlerPcms2 : XmlFormatHandler
{
    public HandlerPcms2(IEnumerable<IXmlConfigParser> parsers) : base("PCMS2", parsers) { }

    protected override bool IsConfigFile(string path) =>
        PackagePath.FileNameOf(path).Equals("problem.xml", StringComparison.OrdinalIgnoreCase);
}
