using System.Diagnostics.CodeAnalysis;
using Parser.Abstractions;

namespace Parser.Handlers;

/// <summary>
/// Справочник обработчиков по имени формата. Чтобы добавить новый формат, достаточно написать
/// обработчик и передать его сюда — существующий код менять не нужно.
/// </summary>
public sealed class FormatHandlerRegistry
{
    private readonly Dictionary<string, IFormatHandler> _handlers;

    public FormatHandlerRegistry(IEnumerable<IFormatHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.FormatName, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<string> FormatNames => _handlers.Keys;

    public bool TryGet(string formatName, [NotNullWhen(true)] out IFormatHandler? handler) =>
        _handlers.TryGetValue(formatName, out handler);
}
