using System.Xml.Linq;

namespace Parser.Parsers;

/// <summary>Короткие безопасные методы чтения атрибутов и вложенных тегов.</summary>
internal static class XmlExtensions
{
    /// <summary>Значение атрибута или null, если его нет или он пустой.</summary>
    public static string? Attr(this XElement element, string name)
    {
        var value = element.Attribute(name)?.Value.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static int? IntAttr(this XElement element, string name) => ValueParser.ToInt(element.Attr(name));

    /// <summary>Текст вложенного тега или null.</summary>
    public static string? ChildText(this XElement element, string name)
    {
        var value = element.Element(name)?.Value.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public static int? ChildInt(this XElement element, string name) => ValueParser.ToInt(element.ChildText(name));

    public static long? ChildLong(this XElement element, string name) => ValueParser.ToLong(element.ChildText(name));
}
