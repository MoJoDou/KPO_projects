namespace Parser.Parsers;

/// <summary>Содержимое valuer.cfg: глобальные опции и группы тестов.</summary>
public sealed class ValuerConfig
{
    public List<string> GlobalOptions { get; } = new();
    public List<ValuerGroup> Groups { get; } = new();
}

public sealed class ValuerGroup
{
    public string Id { get; set; } = "";
    public List<int> Tests { get; } = new();
    public int? Score { get; set; }            // балл за всю группу
    public int? TestScore { get; set; }        // балл за один тест
    public List<string> Requires { get; } = new();
    public List<string> OtherOptions { get; } = new();   // offline, skip, stat_to_users и др.
}
