namespace Parser.Models;

/// <summary>Решение, приложенное к задаче (авторское, неверное и т.п.).</summary>
public sealed class SolutionInfo
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public string? Tag { get; set; }
}
