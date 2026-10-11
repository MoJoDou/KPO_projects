using System.Xml.Linq;
using Parser.Models;

namespace Parser.Abstractions;

/// <summary>Разбирает один XML-файл с описанием задачи в общую модель.</summary>
public interface IXmlConfigParser
{
    /// <summary>Подходит ли этот парсер к документу (нужно, когда у формата несколько вариантов файла).</summary>
    bool CanParse(XDocument document);

    ProblemInfo Parse(XDocument document);
}
