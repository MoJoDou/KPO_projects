namespace Parser.Models;

/// <summary>Один тест задачи. Поля, которых нет в формате, остаются null.</summary>
public sealed class TestInfo
{
    public int Number { get; set; }
    public string? TestSet { get; set; }
    public string? GroupId { get; set; }
    public int? Points { get; set; }
    public string? InputFile { get; set; }
    public string? AnswerFile { get; set; }
    public bool IsSample { get; set; }
    public string? Note { get; set; }
}
