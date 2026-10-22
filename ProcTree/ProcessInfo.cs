namespace ProcTree;

public sealed record ProcessInfo(int Pid, int ParentPid, string Name);
