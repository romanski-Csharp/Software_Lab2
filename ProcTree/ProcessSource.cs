using System.ComponentModel;
using System.Text.RegularExpressions;

namespace ProcTree;

public static class ProcessSource
{
    public static string LastSource { get; private set; } = "";

    public static List<ProcessInfo> GetAll()
    {
        if (!OperatingSystem.IsWindows())
        {
            LastSource = "ps -e -o pid,ppid,comm";
            return FromPs();
        }

        List<ProcessInfo> processes = new();
        try
        {
            processes = FromWmic();
            LastSource = "wmic process get ProcessId,ParentProcessId,Name";
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
        }

        if (processes.Count == 0)
        {
            processes = FromPowerShell();
            LastSource = "powershell Get-CimInstance Win32_Process";
        }
        return processes;
    }

    public static CommandResult Kill(int pid)
    {
        return OperatingSystem.IsWindows()
            ? CommandRunner.Run("taskkill", $"/F /PID {pid}")
            : CommandRunner.Run("kill", $"-9 {pid}");
    }

    public static bool IsAlreadyExited(CommandResult result)
    {
        return OperatingSystem.IsWindows()
            ? result.ExitCode == 128
            : result.Error.Contains("No such process");
    }

    private static List<ProcessInfo> FromWmic()
    {
        CommandResult result = CommandRunner.Run("wmic", "process get ProcessId,ParentProcessId,Name");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"wmic failed: {result.Error}");
        }
        return ParseTable(result.Output);
    }

    private static List<ProcessInfo> ParseTable(string table)
    {
        var processes = new List<ProcessInfo>();
        string[] lines = CleanOutput(table).Replace("\r", "").Split('\n');

        int header = Array.FindIndex(lines, l => Regex.IsMatch(l, @"\bProcessId\b"));
        if (header < 0)
        {
            return processes;
        }

        string headerLine = lines[header];
        int nameStart = ColumnStart(headerLine, "Name");
        int ppidStart = ColumnStart(headerLine, "ParentProcessId");
        int pidStart = ColumnStart(headerLine, "ProcessId");
        if (nameStart < 0 || ppidStart < 0 || pidStart < 0)
        {
            return processes;
        }
        int[] starts = { nameStart, ppidStart, pidStart };

        for (int i = header + 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            string name = Cell(line, nameStart, starts);
            if (int.TryParse(Cell(line, ppidStart, starts), out int ppid)
                && int.TryParse(Cell(line, pidStart, starts), out int pid))
            {
                processes.Add(new ProcessInfo(pid, ppid, name));
            }
        }
        return processes;
    }

    private static string CleanOutput(string output)
    {
        return output.Replace("\0", "").Replace("\uFEFF", "").Replace("\uFFFD", "");
    }

    private static int ColumnStart(string headerLine, string column)
    {
        Match match = Regex.Match(headerLine, $@"\b{column}\b");
        return match.Success ? match.Index : -1;
    }

    private static string Cell(string line, int start, int[] allStarts)
    {
        if (start >= line.Length)
        {
            return "";
        }
        int end = allStarts.Where(s => s > start).DefaultIfEmpty(line.Length).Min();
        end = Math.Min(end, line.Length);
        return line[start..end].Trim();
    }

    private static List<ProcessInfo> FromPowerShell()
    {
        CommandResult result = CommandRunner.Run("powershell",
            "-NoProfile -Command \"Get-CimInstance Win32_Process | " +
            "Select-Object ProcessId,ParentProcessId,Name | ConvertTo-Csv -NoTypeInformation\"");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"powershell failed: {result.Error}");
        }
        return ParseCsv(result.Output);
    }

    private static List<ProcessInfo> FromPs()
    {
        CommandResult result = CommandRunner.Run("ps", "-e -o pid=,ppid=,comm=");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"ps failed: {result.Error}");
        }

        var processes = new List<ProcessInfo>();
        foreach (string line in result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = line.Trim().Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 && int.TryParse(parts[0], out int pid) && int.TryParse(parts[1], out int ppid))
            {
                processes.Add(new ProcessInfo(pid, ppid, parts[2].Trim()));
            }
        }
        return processes;
    }

    private static List<ProcessInfo> ParseCsv(string csv)
    {
        var processes = new List<ProcessInfo>();
        csv = CleanOutput(csv);
        string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int header = Array.FindIndex(lines, l => l.Contains("ProcessId"));
        if (header < 0)
        {
            return processes;
        }

        string[] columns = SplitCsvLine(lines[header]);
        int pidColumn = Array.IndexOf(columns, "ProcessId");
        int ppidColumn = Array.IndexOf(columns, "ParentProcessId");
        int nameColumn = Array.IndexOf(columns, "Name");

        for (int i = header + 1; i < lines.Length; i++)
        {
            string[] values = SplitCsvLine(lines[i]);
            if (values.Length < columns.Length)
            {
                continue;
            }
            if (int.TryParse(values[pidColumn], out int pid) && int.TryParse(values[ppidColumn], out int ppid))
            {
                processes.Add(new ProcessInfo(pid, ppid, values[nameColumn]));
            }
        }
        return processes;
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',').Select(v => v.Trim().Trim('"')).ToArray();
    }
}
