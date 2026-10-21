using System.Diagnostics;

namespace ProcTree;

/// <summary>
/// Результат виконання дочірнього процесу.
/// </summary>
public sealed record CommandResult(int ExitCode, string Output, string Error);

/// <summary>
/// Запускає іншу програму як дочірній процес і зчитує її stdout та stderr.
/// </summary>
public static class CommandRunner
{
    public static CommandResult Run(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,   // stdout дочірнього процесу -> нам
            RedirectStandardError = true,    // stderr дочірнього процесу -> нам
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'");

        // stderr читаємо асинхронно, а stdout - до кінця, і лише потім чекаємо завершення.
        // Якщо спочатку викликати WaitForExit, дочірній процес може заблокуватися
        // на переповненій трубі (pipe), і обидва процеси чекатимуть один на одного (deadlock).
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return new CommandResult(process.ExitCode, output, errorTask.Result);
    }
}
