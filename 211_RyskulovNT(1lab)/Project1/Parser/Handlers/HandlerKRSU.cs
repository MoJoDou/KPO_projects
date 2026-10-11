using Parser.Abstractions;
using Parser.Infrastructure;

namespace Parser.Handlers;

/// <summary>Формат KRSU: описание задачи лежит в файле testinfo.xml (их может быть несколько — по одному на задачу).</summary>
public sealed class HandlerKRSU : XmlFormatHandler
{
    public HandlerKRSU(IXmlConfigParser parser) : base("KRSU", new[] { parser }) { }

    protected override bool IsConfigFile(string path) =>
        PackagePath.FileNameOf(path).Equals("testinfo.xml", StringComparison.OrdinalIgnoreCase);
}
