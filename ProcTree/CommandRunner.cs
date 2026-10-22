using System.Diagnostics;

namespace ProcTree;

public sealed record CommandResult(int ExitCode, string Output, string Error);

public static class CommandRunner
{
    public static CommandResult Run(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{fileName}'");

        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return new CommandResult(process.ExitCode, output, errorTask.Result);
    }
}
