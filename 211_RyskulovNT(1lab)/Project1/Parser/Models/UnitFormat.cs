namespace Parser.Models;

/// <summary>Красивый вывод времени и размеров памяти.</summary>
public static class UnitFormat
{
    private const long Megabyte = 1024 * 1024;

    public static string Milliseconds(int? ms)
    {
        if (ms is null) return "не задан";
        return ms % 1000 == 0 ? $"{ms} мс ({ms / 1000} с)" : $"{ms} мс";
    }

    public static string Bytes(long? bytes)
    {
        if (bytes is null) return "не задан";
        if (bytes % Megabyte == 0) return $"{bytes / Megabyte} МБ ({bytes} байт)";
        if (bytes % 1024 == 0) return $"{bytes / 1024} КБ ({bytes} байт)";
        return $"{bytes} байт";
    }
}
