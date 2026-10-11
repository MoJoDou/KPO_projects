using Parser.Models;

namespace Parser.Abstractions;

/// <summary>Выводит результат разбора. Консоль, файл, JSON — это просто разные реализации.</summary>
public interface IProblemPrinter
{
    void Print(string packageName, IReadOnlyList<ProblemInfo> problems);
}
