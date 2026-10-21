using System.Text;

namespace ProcTree;

/// <summary>
/// Лабораторна робота №2, завдання 2, варіант 4.
/// Відображає дерево процесів, починаючи з заданого PID, або зупиняє все дерево:
/// спочатку найнижчий рівень, батьківський процес - останній.
/// Список процесів і зупинка виконуються запуском інших програм (wmic/powershell, taskkill).
///
/// Команди:
///   ProcTree tree &lt;pid&gt;
///   ProcTree kill &lt;pid&gt; [--dry-run] [--yes]
///   ProcTree help
/// </summary>
public static class Program
{
    private const int ExitOk = 0;
    private const int ExitUsage = 2;
    private const int ExitNotFound = 3;
    private const int ExitKillFailed = 4;

    // Процеси, які не можна зупиняти: System Idle Process (0) та System (4) у Windows, init (1) у Linux
    private static readonly HashSet<int> ProtectedPids = new() { 0, 1, 4 };

    public static int Main(string[] args)
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;   // для символів дерева ├── └──
        }
        catch (IOException)
        {
        }

        if (args.Length == 0 || args[0] is "help" or "--help" or "-h")
        {
            PrintHelp();
            return args.Length == 0 ? ExitUsage : ExitOk;
        }

        string command = args[0];
        if (command == "list")
        {
            return ListProcesses();
        }
        if (command is not ("tree" or "kill"))
        {
            return Fail($"unknown command '{command}'", ExitUsage);
        }
        if (args.Length < 2 || !int.TryParse(args[1], out int pid) || pid < 0)
        {
            return Fail("expected a process id, e.g. 'ProcTree tree 1234'", ExitUsage);
        }

        bool dryRun = false;
        bool noConfirm = false;
        foreach (string option in args.Skip(2))
        {
            switch (option)
            {
                case "--dry-run": dryRun = true; break;
                case "--yes": noConfirm = true; break;
                default: return Fail($"unknown option '{option}'", ExitUsage);
            }
        }

        List<ProcessInfo> processes = ProcessSource.GetAll();
        if (processes.Count == 0)
        {
            return Fail($"could not read the process list (source: {ProcessSource.LastSource})", ExitNotFound);
        }
        ProcessTree tree = new(processes);
        if (tree.Find(pid) == null)
        {
            return Fail($"process {pid} not found", ExitNotFound);
        }

        return command == "tree"
            ? ShowTree(tree, pid)
            : KillTree(tree, pid, dryRun, noConfirm);
    }

    private static int ListProcesses()
    {
        List<ProcessInfo> processes = ProcessSource.GetAll();
        Console.WriteLine($"Source: {ProcessSource.LastSource}");
        Console.WriteLine($"Processes: {processes.Count}");
        Console.WriteLine($"{"PID",8} {"PPID",8}  NAME");
        foreach (ProcessInfo process in processes.OrderBy(p => p.Pid))
        {
            Console.WriteLine($"{process.Pid,8} {process.ParentPid,8}  {process.Name}");
        }
        return processes.Count > 0 ? ExitOk : ExitNotFound;
    }

    private static int ShowTree(ProcessTree tree, int pid)
    {
        tree.Print(pid, Console.Out);
        return ExitOk;
    }

    private static int KillTree(ProcessTree tree, int pid, bool dryRun, bool noConfirm)
    {
        int self = Environment.ProcessId;
        List<(ProcessInfo Process, int Depth)> order = tree.GetKillOrder(pid);

        foreach ((ProcessInfo process, _) in order)
        {
            if (process.Pid == self)
            {
                return Fail("refusing to kill a tree that contains ProcTree itself", ExitUsage);
            }
            if (ProtectedPids.Contains(process.Pid))
            {
                return Fail($"refusing to kill system process {process.Pid} {process.Name}", ExitUsage);
            }
        }

        tree.Print(pid, Console.Out);
        Console.WriteLine();
        Console.WriteLine("Kill order (deepest level first, root last):");
        for (int i = 0; i < order.Count; i++)
        {
            Console.WriteLine($"  {i + 1}. [level {order[i].Depth}] {order[i].Process.Pid} {order[i].Process.Name}");
        }

        if (dryRun)
        {
            Console.WriteLine("Dry run: nothing was killed.");
            return ExitOk;
        }

        if (!noConfirm)
        {
            Console.Write($"Kill {order.Count} process(es)? [y/N] ");
            string? answer = Console.ReadLine();
            if (answer?.Trim().ToLowerInvariant() is not ("y" or "yes"))
            {
                Console.WriteLine("Cancelled.");
                return ExitOk;
            }
        }

        int failed = 0;
        foreach ((ProcessInfo process, int depth) in order)
        {
            CommandResult result = ProcessSource.Kill(process.Pid);
            if (result.ExitCode == 0)
            {
                Console.WriteLine($"killed   [level {depth}] {process.Pid} {process.Name}");
            }
            else if (ProcessSource.IsAlreadyExited(result))
            {
                // Батьківський процес міг завершитися сам, коли зникли його дочірні процеси
                Console.WriteLine($"exited   [level {depth}] {process.Pid} {process.Name} (already finished)");
            }
            else
            {
                failed++;
                string reason = (result.Error + result.Output).Trim();
                Console.Error.WriteLine($"failed   [level {depth}] {process.Pid} {process.Name}: {reason}");
            }
        }

        return failed == 0 ? ExitOk : ExitKillFailed;
    }

    private static int Fail(string message, int exitCode)
    {
        Console.Error.WriteLine($"ProcTree: {message}");
        return exitCode;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  ProcTree list                            show all processes and where the list came from");
        Console.WriteLine("  ProcTree tree <pid>                      show the process tree starting from <pid>");
        Console.WriteLine("  ProcTree kill <pid> [--dry-run] [--yes]  kill the whole tree, deepest level first");
        Console.WriteLine("Options:");
        Console.WriteLine("  --dry-run   only show the kill order");
        Console.WriteLine("  --yes       do not ask for confirmation");
    }
}
