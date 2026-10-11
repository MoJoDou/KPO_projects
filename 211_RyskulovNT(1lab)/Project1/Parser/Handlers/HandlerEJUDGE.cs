using Parser.Abstractions;
using Parser.Infrastructure;
using Parser.Models;
using Parser.Parsers;

namespace Parser.Handlers;

/// <summary>
/// Формат EJUDGE: задача описана двумя текстовыми файлами рядом друг с другом —
/// problem.cfg (лимиты, имена файлов) и valuer.cfg (группы тестов и баллы).
/// </summary>
public sealed class HandlerEJUDGE : IFormatHandler
{
    private readonly ITextConfigParser<EjudgeProblemConfig> _problemParser;
    private readonly ITextConfigParser<ValuerConfig> _valuerParser;

    public HandlerEJUDGE(ITextConfigParser<EjudgeProblemConfig> problemParser, ITextConfigParser<ValuerConfig> valuerParser)
    {
        _problemParser = problemParser;
        _valuerParser = valuerParser;
    }

    public string FormatName => "EJUDGE";

    public IReadOnlyList<ProblemInfo> Handle(IPackage package)
    {
        var configs = package.Files
            .Where(f => PackagePath.FileNameOf(f).Equals("problem.cfg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (configs.Count == 0)
            throw new PackageException($"В пакете '{package.Name}' не найден файл problem.cfg (формат {FormatName}).");

        return configs.Select(path => BuildProblem(package, path)).ToList();
    }

    private ProblemInfo BuildProblem(IPackage package, string configPath)
    {
        string dir = PackagePath.DirectoryOf(configPath);
        var cfg = _problemParser.Parse(package.ReadAllText(configPath));

        var info = new ProblemInfo
        {
            Format = FormatName,
            ConfigFile = configPath,
            Location = dir,
            Id = cfg.Get("internal_name"),
            Title = cfg.Get("long_name"),
            Checker = cfg.Get("check_cmd"),
            Interactor = cfg.Get("interactor_cmd"),
            InputFile = cfg.GetFlag("use_stdin") ? "stdin" : cfg.Get("input_file"),
            OutputFile = cfg.GetFlag("use_stdout") ? "stdout" : cfg.Get("output_file"),
            TimeLimitMs = ValueParser.ToMilliseconds(cfg.Get("time_limit_millis"), plainIsSeconds: false)
                          ?? ValueParser.ToMilliseconds(cfg.Get("time_limit"), plainIsSeconds: true),
            MemoryLimitBytes = ValueParser.ToBytes(cfg.Get("max_vm_size"), plainUnitBytes: 1)
        };

        info.AddProperty("Название (англ.)", cfg.Get("long_name_en"));
        info.AddProperty("Идентификатор в Polygon", cfg.Get("extid"));
        info.AddProperty("Ревизия", cfg.Get("revision"));
        info.AddProperty("Файл правильного ответа", cfg.Get("use_corr") == null ? null : cfg.GetFlag("use_corr") ? "используется" : "не используется");
        info.AddProperty("Режим testlib", cfg.Get("enable_testlib_mode") == null ? null : cfg.GetFlag("enable_testlib_mode") ? "включён" : "выключен");

        if (cfg.Get("solution_cmd") is { } solution)
            info.Solutions.Add(new SolutionInfo { Name = solution, Tag = "авторское" });

        ReadTests(package, dir, cfg, info);
        return info;
    }

    private void ReadTests(IPackage package, string dir, EjudgeProblemConfig cfg, ProblemInfo info)
    {
        string valuerPath = PackagePath.Combine(dir, "valuer.cfg");
        if (!package.Exists(valuerPath))
        {
            info.AddProperty("Группы тестов", "valuer.cfg не найден — группы и баллы не заданы");
            return;
        }

        var valuer = _valuerParser.Parse(package.ReadAllText(valuerPath));
        info.AddProperty("Глобальные опции valuer", string.Join(", ", valuer.GlobalOptions));

        string testDir = cfg.Get("test_dir") ?? "tests";
        string? testPattern = cfg.Get("test_pat");
        string? answerPattern = cfg.Get("corr_pat");
        var tests = new SortedDictionary<int, TestInfo>();

        foreach (var g in valuer.Groups)
        {
            var group = new TestGroup
            {
                Id = g.Id,
                // «score» — балл за всю группу; если его нет, группа стоит test_score за каждый тест
                Points = g.Score ?? g.TestScore * g.Tests.Count,
                Note = g.OtherOptions.Count > 0 ? string.Join("; ", g.OtherOptions) : null
            };
            group.Prerequisites.AddRange(g.Requires);
            group.TestNumbers.AddRange(g.Tests);
            info.Groups.Add(group);

            foreach (int n in g.Tests)
            {
                if (tests.ContainsKey(n)) continue;   // тест уже попал в другую группу
                tests[n] = new TestInfo
                {
                    Number = n,
                    GroupId = g.Id,
                    Points = g.TestScore,
                    InputFile = testPattern == null ? null : $"{testDir}/{NamePattern.Apply(testPattern, n)}",
                    AnswerFile = answerPattern == null ? null : $"{testDir}/{NamePattern.Apply(answerPattern, n)}"
                };
            }
        }
        info.Tests.AddRange(tests.Values);
    }
}
