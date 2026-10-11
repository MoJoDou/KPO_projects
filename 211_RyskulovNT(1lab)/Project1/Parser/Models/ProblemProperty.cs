namespace Parser.Models;

/// <summary>
/// Дополнительное свойство задачи вида «название: значение».
/// Нужно, чтобы форматы могли добавлять свои уникальные поля, не меняя общую модель.
/// </summary>
public sealed record ProblemProperty(string Name, string Value);
