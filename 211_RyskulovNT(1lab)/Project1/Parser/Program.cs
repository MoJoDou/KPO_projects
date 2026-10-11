using System.Text;
using Parser.Abstractions;
using Parser.Handlers;
using Parser.Infrastructure;
using Parser.Output;
using Parser.Parsers;

namespace Parser;

internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);   // нужна для Windows-1251 в файлах CATS

        var registry = CreateRegistry();

        // Параметры: [FORMAT] [PACKAGE] и необязательный флаг --all
        var positional = args.Where(a => !a.StartsWith("--")).ToList();
        bool showAllTests = args.Any(a => a.Equals("--all", StringComparison.OrdinalIgnoreCase));

        if (positional.Count != 2)
        {
            PrintUsage(registry);
            return 1;
        }

        string format = positional[0];
        string packagePath = positional[1];

        if (!registry.TryGet(format, out var handler))
        {
            Console.Error.WriteLine($"Неизвестный формат '{format}'. Доступные форматы: {string.Join(", ", registry.FormatNames)}.");
            return 1;
        }

        try
        {
            IProblemPrinter printer = new ConsoleProblemPrinter(Console.Out, showAllTests ? int.MaxValue : 10);

            using IPackage package = new ZipPackage(packagePath);
            var problems = handler.Handle(package);
            printer.Print(package.Name, problems);
            return 0;
        }
        catch (PackageException ex)
        {
            Console.Error.WriteLine("Ошибка: " + ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Непредвиденная ошибка: " + ex.Message);
            return 2;
        }
    }

    /// <summary>Единственное место, где перечислены все форматы и их парсеры.</summary>
    private static FormatHandlerRegistry CreateRegistry() => new(new IFormatHandler[]
    {
        new HandlerKRSU(new KrsuTestInfoParser()),
        new HandlerCATS(new CatsProblemParser()),
        new HandlerPcms2(new IXmlConfigParser[] { new Pcms2PolygonParser(), new Pcms2IoiParser() }),
        new HandlerEJUDGE(new EjudgeProblemCfgParser(), new EjudgeValuerParser())
    });

    private static void PrintUsage(FormatHandlerRegistry registry)
    {
        Console.WriteLine("Использование: Parser [FORMAT] [PACKAGE] [--all]");
        Console.WriteLine($"  FORMAT   формат пакета: {string.Join(", ", registry.FormatNames)}");
        Console.WriteLine("  PACKAGE  путь к ZIP-файлу пакета");
        Console.WriteLine("  --all    показать все тесты (по умолчанию печатаются первые 10)");
        Console.WriteLine("Пример: Parser KRSU KRSU.zip");
    }
}
