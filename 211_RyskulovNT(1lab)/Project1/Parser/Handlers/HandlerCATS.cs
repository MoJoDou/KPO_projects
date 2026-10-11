using Parser.Abstractions;
using Parser.Infrastructure;

namespace Parser.Handlers;

/// <summary>
/// Формат CATS: в корне пакета ровно один XML-файл с задачей.
/// Если архиватор положил всё в общую папку, «корнем» считается самый мелкий уровень, где есть XML.
/// </summary>
public sealed class HandlerCATS : XmlFormatHandler
{
    public HandlerCATS(IXmlConfigParser parser) : base("CATS", new[] { parser }) { }

    protected override bool IsConfigFile(string path) =>
        path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<string> SelectConfigFiles(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0) return candidates;
        int shallowest = candidates.Min(PackagePath.Depth);
        return candidates.Where(c => PackagePath.Depth(c) == shallowest);
    }
}
