using Parser.Models;

namespace Parser.Abstractions;

/// <summary>Обработчик одного формата пакета: находит в пакете задачи и разбирает их.</summary>
public interface IFormatHandler
{
    /// <summary>Имя формата, как его вводят в командной строке (KRSU, CATS...).</summary>
    string FormatName { get; }

    /// <summary>В одном пакете может лежать несколько задач, поэтому возвращается список.</summary>
    IReadOnlyList<ProblemInfo> Handle(IPackage package);
}
