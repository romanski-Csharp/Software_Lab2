namespace ProcTree;

/// <summary>
/// Дерево процесів, побудоване зі списку процесів за ідентифікатором батька (PPID).
/// </summary>
public sealed class ProcessTree
{
    private readonly Dictionary<int, ProcessInfo> _byPid = new();
    private readonly Dictionary<int, List<ProcessInfo>> _children = new();

    public ProcessTree(IEnumerable<ProcessInfo> processes)
    {
        foreach (ProcessInfo process in processes)
        {
            _byPid[process.Pid] = process;
        }
        foreach (ProcessInfo process in _byPid.Values)
        {
            // Процес не може бути батьком самого себе (у Windows так виглядає процес з PID 0)
            if (process.ParentPid == process.Pid)
            {
                continue;
            }
            if (!_children.TryGetValue(process.ParentPid, out List<ProcessInfo>? list))
            {
                list = new List<ProcessInfo>();
                _children[process.ParentPid] = list;
            }
            list.Add(process);
        }
        foreach (List<ProcessInfo> list in _children.Values)
        {
            list.Sort((a, b) => a.Pid.CompareTo(b.Pid));
        }
    }

    public ProcessInfo? Find(int pid) => _byPid.GetValueOrDefault(pid);

    /// <summary>
    /// Повертає процеси піддерева разом з глибиною (корінь має глибину 0).
    /// Обхід у ширину. Множина visited захищає від циклів: у Windows номери PID
    /// використовуються повторно, і "батьком" може виявитися чужий процес.
    /// </summary>
    public List<(ProcessInfo Process, int Depth)> GetSubtree(int rootPid)
    {
        var result = new List<(ProcessInfo, int)>();
        ProcessInfo? root = Find(rootPid);
        if (root == null)
        {
            return result;
        }

        var visited = new HashSet<int> { root.Pid };
        var queue = new Queue<(ProcessInfo, int)>();
        queue.Enqueue((root, 0));

        while (queue.Count > 0)
        {
            (ProcessInfo current, int depth) = queue.Dequeue();
            result.Add((current, depth));
            foreach (ProcessInfo child in ChildrenOf(current.Pid))
            {
                if (visited.Add(child.Pid))
                {
                    queue.Enqueue((child, depth + 1));
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Порядок зупинки: спочатку найглибший рівень, батьківський (кореневий) процес - останній.
    /// </summary>
    public List<(ProcessInfo Process, int Depth)> GetKillOrder(int rootPid)
    {
        return GetSubtree(rootPid)
            .OrderByDescending(item => item.Depth)
            .ThenBy(item => item.Process.Pid)
            .ToList();
    }

    /// <summary>
    /// Виводить дерево у вигляді:
    ///   1234 cmd.exe
    ///   ├── 2001 PING.EXE
    ///   └── 2002 cmd.exe
    ///       └── 3001 PING.EXE
    /// </summary>
    public void Print(int rootPid, TextWriter output)
    {
        ProcessInfo? root = Find(rootPid);
        if (root == null)
        {
            return;
        }
        output.WriteLine($"{root.Pid} {root.Name}");
        PrintChildren(root.Pid, "", new HashSet<int> { root.Pid }, output);
    }

    private void PrintChildren(int pid, string indent, HashSet<int> visited, TextWriter output)
    {
        List<ProcessInfo> children = ChildrenOf(pid).Where(c => !visited.Contains(c.Pid)).ToList();
        for (int i = 0; i < children.Count; i++)
        {
            ProcessInfo child = children[i];
            bool isLast = i == children.Count - 1;
            visited.Add(child.Pid);
            output.WriteLine($"{indent}{(isLast ? "└── " : "├── ")}{child.Pid} {child.Name}");
            PrintChildren(child.Pid, indent + (isLast ? "    " : "│   "), visited, output);
        }
    }

    private IEnumerable<ProcessInfo> ChildrenOf(int pid)
    {
        return _children.TryGetValue(pid, out List<ProcessInfo>? list) ? list : Enumerable.Empty<ProcessInfo>();
    }
}
