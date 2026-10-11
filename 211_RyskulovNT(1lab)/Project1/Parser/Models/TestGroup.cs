namespace Parser.Models;

/// <summary>Группа тестов (подзадача) с баллами и зависимостями от других групп.</summary>
public sealed class TestGroup
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? TestSet { get; set; }
    public int? Points { get; set; }
    public List<string> Prerequisites { get; } = new();
    public List<int> TestNumbers { get; } = new();
    public string? Note { get; set; }
}
