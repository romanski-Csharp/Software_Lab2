using System.Text;
using System.Text.RegularExpressions;

namespace MaskWords;

public static class Program
{
    private const string Mask = "xxxxx";
    private const char MaskChar = 'x';

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
                    Console.Error.WriteLine($"MaskWords: unknown argument '{arg}'. Use --help.");
                    return 2;
            }
        }

        SetUtf8Encoding();

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
        try
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
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
