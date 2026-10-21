using System.Text;
using System.Text.RegularExpressions;

namespace MaskWords;

/// <summary>
/// Лабораторна робота №2, завдання 1, варіант 4.
/// Читає текст зі stdin і виводить у stdout ті самі дані, але кожне слово,
/// що містить символ '@', замінюється на "xxxxx".
/// Параметр --keep-len зберігає довжину слова: "a@b.c" -> "xxxxx" (5 символів),
/// "roman@gmail.com" -> "xxxxxxxxxxxxxxx" (15 символів).
/// Слова розділені одним або кількома пробілами, пробіли зберігаються як є.
/// </summary>
public static class Program
{
    private const string Mask = "xxxxx";
    private const char MaskChar = 'x';

    // Слово - це послідовність символів без пробілів
    private static readonly Regex WordRegex = new("[^ ]+", RegexOptions.Compiled);

    public static int Main(string[] args)
    {
        bool keepLength = false;

        foreach (string arg in args)
        {
            switch (arg)
            {
                case "--keep-len":
                    keepLength = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;
                default:
                    // Помилки пишемо в stderr, щоб вони не потрапили в результат (stdout)
                    Console.Error.WriteLine($"MaskWords: unknown argument '{arg}'. Use --help.");
                    return 2;
            }
        }

        SetUtf8Encoding();

        // Читаємо рядок за рядком: програма працює з даними будь-якого розміру
        // і відразу передає результат далі по pipe
        string? line;
        while ((line = Console.In.ReadLine()) != null)
        {
            Console.Out.WriteLine(MaskLine(line, keepLength));
        }

        return 0;
    }

    public static string MaskLine(string line, bool keepLength)
    {
        return WordRegex.Replace(line, match =>
        {
            string word = match.Value;
            if (!word.Contains('@'))
            {
                return word;
            }
            return keepLength ? new string(MaskChar, word.Length) : Mask;
        });
    }

    private static void SetUtf8Encoding()
    {
        // Щоб кирилиця правильно проходила через pipe (у cmd ще потрібен chcp 65001)
        try
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
            // Без консолі (наприклад, при запуску з іншої програми) кодування змінити не можна
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: MaskWords [--keep-len] < input > output");
        Console.WriteLine("Reads text from stdin and replaces every word that contains '@' with 'xxxxx'.");
        Console.WriteLine("  --keep-len   replace the word with as many 'x' as it has characters");
        Console.WriteLine("  --help       show this help");
    }
}
