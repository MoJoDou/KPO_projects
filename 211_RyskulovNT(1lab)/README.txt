ФИО: Рыскулов Нияз Талантбекович
Группа: ЕПИ-2-2

Система: Microsoft Visual Studio 2022
ОС: Windows 10
Аппаратура: Laptop Acer Aspire E5-576G


# Задание 1. Парсер файлов задач

Консольное приложение на C# (.NET 8). Принимает формат и ZIP-пакет, находит в пакете описание задачи
и печатает информацию о ней: название, лимиты, проверяющую программу, группы тестов, баллы, сами тесты.

```
Разбор пакета KRSU [+]
Разбор пакета CATS [+]
Разбор пакета EJUDGE [+]
Разбор пакета PCMS2 [+]
```

## Запуск

```
Parser.exe [FORMAT] [PACKAGE] [--all]
```

| Параметр  | Значение |
|-----------|----------|
| `FORMAT`  | `KRSU`, `CATS`, `PCMS2` или `EJUDGE` (регистр не важен) |
| `PACKAGE` | путь к ZIP-файлу пакета |
| `--all`   | необязательный: показать все тесты (по умолчанию печатаются первые 10) |

Примеры:

```
Parser.exe KRSU KRSU.zip
Parser.exe CATS CATS.zip
Parser.exe PCMS2 PCMS2.zip --all
Parser.exe EJUDGE EJUDGE.zip --all
Parser.exe EJUDGE C:\packages\EJUDGE.zip (этот пример пути к архиву)
```

Сборка и запуск из консоли: `dotnet run -- KRSU KRSU.zip`.
В Visual Studio аргументы задаются в «Свойства проекта → Отладка → Открыть пользовательский интерфейс
профилей запуска → Аргументы командной строки». ZIP-файлы, лежащие рядом с проектом, копируются
в папку с exe автоматически, поэтому достаточно писать `KRSU KRSU.zip`.

Код возврата: `0` — успех, `1` — ошибка пользователя (неверный формат, нет файла, в пакете нет описания задачи),
`2` — непредвиденная ошибка.

## Структура проекта

```
Parser/
  Program.cs                      точка входа и единственное место, где собраны все форматы
  Abstractions/                   интерфейсы
    IPackage.cs                     доступ к файлам пакета
    IFormatHandler.cs               обработчик формата
    IXmlConfigParser.cs             парсер XML-описания
    ITextConfigParser.cs            парсер текстового конфига
    IProblemPrinter.cs              вывод результата
  Models/                         общая модель задачи (ProblemInfo, TestInfo, TestGroup, ...)
  Infrastructure/                 ZipPackage, PackagePath, PackageException
  Handlers/                       по одному обработчику на формат
    XmlFormatHandler.cs             общий алгоритм для XML-форматов
    HandlerKRSU.cs  HandlerCATS.cs  HandlerPcms2.cs  HandlerEJUDGE.cs
    FormatHandlerRegistry.cs        поиск обработчика по имени формата
  Parsers/                        разбор конкретных файлов
    KrsuTestInfoParser.cs           testinfo.xml
    CatsProblemParser.cs            XML задачи CATS
    Pcms2PolygonParser.cs           problem.xml, вариант Polygon
    Pcms2IoiParser.cs               problem.xml, олимпиадный вариант (judging/script)
    EjudgeProblemCfgParser.cs       problem.cfg
    EjudgeValuerParser.cs           valuer.cfg
    ValueParser, RangeSpec, NamePattern, XmlExtensions — общие помощники
  Output/ConsoleProblemPrinter.cs вывод в консоль
```

### Как это работает

1. `Program` создаёт `ZipPackage` и находит обработчик формата в `FormatHandlerRegistry`.
2. Обработчик ищет в пакете файлы описания (`testinfo.xml`, XML с тегом `<CATS>`, `problem.xml`, `problem.cfg`).
   В одном пакете может быть несколько задач — все они разбираются и выводятся.
3. Парсеры превращают файлы в единую модель `ProblemInfo`. Время хранится в миллисекундах,
   память — в байтах (в форматах они разные: секунды, мс, МБ, «512M»).
4. `ConsoleProblemPrinter` печатает модель. О форматах он ничего не знает.

### Принципы SOLID

| Принцип | Где виден |
|---------|-----------|
| **S** — одна причина для изменения | Обработчик ищет файлы, парсер разбирает файл, принтер печатает, `ZipPackage` читает архив. |
| **O** — открыт для расширения | Новый формат = новый класс-обработчик (и парсер) + одна строка в `Program.CreateRegistry`. Остальной код не меняется. Новый вариант `problem.xml` = ещё один `IXmlConfigParser`. Новый вывод (JSON, файл) = ещё один `IProblemPrinter`. |
| **L** — подстановка | Любой `IFormatHandler` можно подставить вместо другого; `HandlerKRSU`, `HandlerCATS`, `HandlerPcms2` без оговорок заменяют `XmlFormatHandler`. |
| **I** — узкие интерфейсы | `IPackage` (4 члена), `IXmlConfigParser` (2 метода), `ITextConfigParser<T>` (1 метод), `IProblemPrinter` (1 метод). |
| **D** — зависимость от абстракций | Обработчики получают парсеры через конструктор и работают с `IPackage`, а не с `ZipFile`. Поэтому их можно проверять на пакете в памяти без ZIP. |

Дополнительно: общий алгоритм XML-форматов вынесен в абстрактный `XmlFormatHandler` (шаблонный метод),
поэтому `HandlerKRSU`, `HandlerCATS`, `HandlerPcms2` состоят из нескольких строк.

### Как добавить новый формат

1. Написать парсер (`IXmlConfigParser` или свой), который заполняет `ProblemInfo`.
2. Написать обработчик: наследник `XmlFormatHandler` (достаточно указать, какие файлы считать описанием)
   или свою реализацию `IFormatHandler`.
3. Добавить `new HandlerXxx(...)` в `Program.CreateRegistry()`.

## Что поддерживается в каждом формате

- **KRSU** — `testinfo.xml`: чекер, интерактор, файл условия, лимиты, версия, режим запуска, группы
  (баллы, зависимости), тесты (вход, ответ, баллы, группа).
- **CATS** — XML с `<CATS>`: название, автор, языки, лимиты (в т. ч. суффиксы `B/K/M`), файлы ввода/вывода,
  проверяющая программа (`stdChecker`, `<Checker>`, `<Import>`), решения, генераторы, ключевые слова,
  картинки, вложения, примеры, наборы тестов (`<Testset>`), тесты через `<Test>`/`<TestRange>`
  (диапазоны, шаблоны `%n`/`%0n`, значения по умолчанию `rank="*"`). Кодировка XML (например, Windows-1251) читается из заголовка.
- **PCMS2** — `problem.xml` в двух вариантах, определяется автоматически:
  Polygon (`names`, `statements`, `testset`, `assets`: чекер, валидаторы, решения с тегами)
  и олимпиадный (`judging/script`: наборы `testset`, `test-group` с `group-bonus` и `require-groups`, `verifier`, `interactor`).
- **EJUDGE** — `problem.cfg` (лимиты, имена файлов, чекер, решение, идентификаторы)
  и `valuer.cfg` (группы, диапазоны тестов, баллы, зависимости, опции) по грамматике из описания формата.

## Допущения

- Время в CATS без суффикса — секунды; в KRSU и Polygon — миллисекунды; в EJUDGE `time_limit` — секунды, `time_limit_millis` — миллисекунды.
- Память в CATS без суффикса — мегабайты; в EJUDGE и PCMS2 — байты.
- Диапазон тестов с шагом в CATS читается как `от-до-шаг`.
- В олимпиадном варианте PCMS2 группы нумеруются по порядку внутри набора с единицы
  (на эти номера ссылается `require-groups`), баллы группы берутся из `group-bonus`.
- Максимальный балл: значение `maxPoints` (CATS), иначе сумма баллов групп, иначе сумма баллов тестов.
- Программа не проверяет, что файлы тестов физически лежат в пакете: она печатает то, что описано в конфигурации.
- Если в пакете несколько задач (например, `KRSU/A/testinfo.xml` и `KRSU/testinfo.xml`), выводятся все.
