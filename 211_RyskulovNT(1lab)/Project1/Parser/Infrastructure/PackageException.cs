namespace Parser.Infrastructure;

/// <summary>Ошибка, текст которой можно показать пользователю: битый архив, неверный формат и т.п.</summary>
public class PackageException : Exception
{
    public PackageException(string message) : base(message) { }

    public PackageException(string message, Exception inner) : base(message, inner) { }
}
