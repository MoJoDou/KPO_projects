namespace Parser.Abstractions;

/// <summary>Разбирает текстовый (не XML) конфиг в объект настроек типа T.</summary>
public interface ITextConfigParser<out T>
{
    T Parse(string text);
}
